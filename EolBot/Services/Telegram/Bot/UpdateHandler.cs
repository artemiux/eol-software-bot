using EolBot.Extensions;
using EolBot.Repositories.Abstract;
using EolBot.Services.Localization.Abstract;
using EolBot.Services.LogReader;
using EolBot.Services.LogReader.Abstract;
using EolBot.Services.Report.Abstract;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace EolBot.Services.Telegram.Bot
{
    class UpdateHandler(
        IOptions<TelegramSettings> telegramOptions,
        ITelegramBotClient bot,
        TelegramSender sender,
        IReport reportService,
        IServiceScopeFactory scopeFactory,
        ILogReader logReader,
        IOptions<LogReaderSettings> logReaderOptions,
        IBackgroundJobClient jobClient,
        ILocalizationService localizer,
        ILogger<UpdateHandler> logger) : IUpdateHandler
    {
        private readonly TelegramSettings _telegramSettings = telegramOptions.Value;
        private readonly LogReaderSettings _logReaderSettings = logReaderOptions.Value;

        private string? _sendingJobId;

        public async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
        {
            if (exception is RequestException)
            {
                logger.LogError("HandleError: {Message}", exception.Message);
                // Cooldown in case of network connection error.
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
            else
            {
                logger.LogError("HandleError: {Exception}", exception);
                if (_telegramSettings.AdminChatId != default)
                {
                    try
                    {
                        // Notify admin about the error.
                        await botClient.SendMessage(
                            chatId: _telegramSettings.AdminChatId,
                            text: $"[{nameof(EolBot)}] HandleError: {exception.Message}",
                            cancellationToken: cancellationToken);
                    }
                    catch { }
                }
            }
        }

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await (update switch
            {
                { Message: { } message } => OnMessage(message),
                { CallbackQuery: { } callbackQuery } => OnCallbackQuery(callbackQuery, cancellationToken),
                _ => UnknownUpdateHandlerAsync(update)
            });
        }

        private async Task OnMessage(Message msg)
        {
            logger.LogInformation("Receive message type: {MessageType}", msg.Type);

            // Process text messages only from users in private chats.
            if (msg.Text is not { } messageText
                || msg.Chat is not { Type: ChatType.Private } chat
                || msg.From is not { } user || user.IsBot == true)
            {
                return;
            }

            bool isAdmin = user.Id == _telegramSettings.AdminChatId;
            Message sentMessage = await (messageText.Split(' ', StringSplitOptions.TrimEntries)[0] switch
            {
                "/report" => Report(user, chat, user.LanguageCode),
                "/subscribe" => Subscribe(user, chat, user.LanguageCode),
                "/unsubscribe" => Unsubscribe(user, chat, user.LanguageCode),
                "/logs" when isAdmin => Logs(chat),
                "/send" when isAdmin => Send(chat, messageText),
                "/stats" when isAdmin => Stats(chat),
                _ => Usage(chat, user.LanguageCode)
            });
            logger.LogInformation("The message was sent with id: {SentMessageId}", sentMessage.Id);
        }

        private async Task<Message> Report(User user, Chat chat, string? lang)
        {
            logger.LogInformation("User {UserId} requested report", user.Id.ToString());

            using var scope = scopeFactory.CreateScope();
            var reportRepository = scope.ServiceProvider.GetRequiredService<IReportRepository>();
            Models.Report? report;
            try
            {
                report = await reportRepository.LastAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to get last report");
                return await bot.SendMessage(chat, localizer["UnknownError", lang]);
            }

            var text = report is null
                ? localizer["ReportNotFound", lang]
                : reportService.Create(report.From, report.To,
                    items: report.Content.Select(x => x.ConvertToReportItem()),
                    lang: user.LanguageCode);
            return await bot.SendMessage(chat, text, parseMode: ParseMode.Html);
        }

        #region Subscription commands
        private async Task<Message> Subscribe(User user, Chat chat, string? lang)
        {
            using var scope = scopeFactory.CreateScope();
            var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            try
            {
                await userRepository.SubscribeAsync(user.Id, user.LanguageCode);
                logger.LogInformation("Subscribed user: {UserId} ({UserFirstName})", user.Id, user.FirstName);
                return await bot.SendMessage(chat, localizer["Subscribed", lang]);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to subscribe user: {UserId} ({UserFirstName})", user.Id, user.FirstName);
                return await bot.SendMessage(chat, localizer["UnknownError", lang]);
            }
        }

        private async Task<Message> Unsubscribe(User user, Chat chat, string? lang)
        {
            using var scope = scopeFactory.CreateScope();
            var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            try
            {
                await userRepository.UnsubscribeAsync(user.Id, user.LanguageCode);
                logger.LogInformation("Unsubscribed user: {UserId} ({UserFirstName})", user.Id, user.FirstName);
                return await bot.SendMessage(chat, localizer["Unsubscribed", lang]);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to unsubscribe user: {UserId} ({UserFirstName})", user.Id, user.FirstName);
                return await bot.SendMessage(chat, localizer["UnknownError", lang]);
            }
        }
        #endregion

        #region Admin commands
        private async Task<Message> Logs(Chat chat)
        {
            var lines = (await logReader.TailAsync("AppData/Logs/", _logReaderSettings.MaxLines)).ToArray();
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Length > _logReaderSettings.MaxLineLength)
                {
                    lines[i] = string.Concat(lines[i].AsSpan(0, _logReaderSettings.MaxLineLength - 3), "...");
                }
            }

            string text = WebUtility.HtmlEncode(
                value: string.Join("\n\n", lines.Length > 0 ? lines : ["No logs found"]));
            if (text.Length > 4085)
            {
                text = string.Concat(text.AsSpan(0, 4082), "...");
            }
            return await bot.SendMessage(chat, $"<pre>{text}</pre>", ParseMode.Html);
        }

        private async Task<Message> Send(Chat chat, string messageText)
        {
            var parts = messageText.Split(' ', StringSplitOptions.TrimEntries);
            if (parts is [_, "start"])
            {
                if (sender.Active)
                {
                    return await bot.SendMessage(chat, "Already in progress.");
                }

                using var scope = scopeFactory.CreateScope();
                var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
                int usersToProcess = await userRepository.GetQueryable().CountAsync(u => u.IsActive);
                return await bot.SendMessage(
                    chatId: chat,
                    text: $"The report will be sent to {usersToProcess} users. Proceed?",
                    replyMarkup: new InlineKeyboardButton[][]
                    {
                        [("Yes", "send start"), ("No", "cancel")]
                    });
            }
            else if (parts is [_, "stop"])
            {
                if (IsSendingJobCompletedOrNotExist())
                {
                    return await bot.SendMessage(chat, "Nothing to stop. Use `/send start` first.");
                }

                return await bot.SendMessage(
                    chatId: chat,
                    text: "Sending the report will be stopped. Proceed?",
                    replyMarkup: new InlineKeyboardButton[][]
                    {
                        [("Yes", "send stop"), ("No", "cancel")]
                    });
            }
            else
            {
                return await bot.SendMessage(
                    chatId: chat,
                    text: "Usage: /send <start|stop>");
            }
        }

        private async Task<Message> Stats(Chat chat)
        {
            using var scope = scopeFactory.CreateScope();
            var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            
            var total = await userRepository.GetTotalAsync();
            var active = await userRepository.GetActiveAsync();
            
            var to = DateTime.UtcNow;
            var from = to.AddDays(-30);
            var lastTotal = await userRepository.GetTotalAsync(from, to);
            var lastActive = await userRepository.GetActiveAsync(from, to);
            
            string text = $"""
                Overall: {active}/{total}
                Last 30 days: {lastActive}/{lastTotal}
                """;
            return await bot.SendMessage(chat, text);
        }

        private bool IsSendingJobCompletedOrNotExist()
        {
            if (_sendingJobId is null)
            {
                return true;
            }

            using var connection = JobStorage.Current.GetConnection();
            var jobData = connection.GetJobData(_sendingJobId);
            return jobData is null || jobData is { State: "Succeeded" or "Deleted" or "Failed" };
        }
        #endregion

        #region Callbacks
        private async Task OnCallbackQuery(CallbackQuery callbackQuery, CancellationToken cancellationToken)
        {
            if (callbackQuery.Message?.Chat is not { } chat)
            {
                return;
            }

            await bot.DeleteMessage(
                chatId: chat,
                messageId: callbackQuery.Message.Id,
                cancellationToken: cancellationToken);

            switch (callbackQuery.Data)
            {
                case "send start":
                    await StartSendingAsync(chat, cancellationToken);
                    break;
                case "send stop":
                    await StopSendingAsync(chat);
                    break;
                default:
                    break;
            }
        }

        private async Task StartSendingAsync(Chat chat, CancellationToken cancellationToken)
        {
            if (sender.Active)
            {
                await bot.SendMessage(chat, "Already in progress.", cancellationToken: cancellationToken);
            }
            else
            {
                await bot.SendMessage(chat, "Start sending...", cancellationToken: cancellationToken);
                _sendingJobId = jobClient.Enqueue<Jobs>(
                    (jobs) => jobs.SendWeeklyReportAsync(default!));
            }
        }

        private async Task StopSendingAsync(Chat chat)
        {
            if (IsSendingJobCompletedOrNotExist())
            {
                await bot.SendMessage(chat, "Nothing to stop. Use `/send start` first.");
            }
            else
            {
                jobClient.Delete(_sendingJobId);
                _sendingJobId = null;
                await bot.SendMessage(chat, "Stopped.");
            }
        }
        #endregion

        private async Task<Message> Usage(Chat chat, string? lang)
        {
            return await bot.SendMessage(chat, localizer["Usage", lang]);
        }

        private Task UnknownUpdateHandlerAsync(Update update)
        {
            logger.LogInformation("Unknown update type: {UpdateType}", update.Type);
            return Task.CompletedTask;
        }
    }
}
