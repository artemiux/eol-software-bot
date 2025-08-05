using EolBot.Repositories.Abstract;
using EolBot.Services.LogReader;
using EolBot.Services.LogReader.Abstract;
using EolBot.Services.Report;
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
        IOptions<ReportSettings> reportOptions,
        ITelegramBotClient bot,
        TelegramSender sender,
        IServiceScopeFactory scopeFactory,
        ILogReader logReader,
        IOptions<LogReaderSettings> logReaderOptions,
        ILogger<UpdateHandler> logger) : IUpdateHandler
    {
        private readonly TelegramSettings _telegramSettings = telegramOptions.Value;
        private readonly ReportSettings _reportSettings = reportOptions.Value;
        private readonly LogReaderSettings _logReaderSettings = logReaderOptions.Value;

        private CancellationTokenSource? _sendingCancellationTokenSource;
        private Task? _sendingTask;

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

            using var scope = scopeFactory.CreateScope();
            var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

            bool isAdmin = user.Id == _telegramSettings.AdminChatId;
            Message sentMessage = await (messageText.Split(' ', StringSplitOptions.TrimEntries)[0] switch
            {
                "/subscribe" => Subscribe(user, chat, userRepository),
                "/unsubscribe" => Unsubscribe(user, chat, userRepository),
                "/logs" when isAdmin => Logs(chat),
                "/send" when isAdmin => Send(chat, messageText, userRepository),
                "/stats" when isAdmin => Stats(chat, userRepository),
                _ => Usage(chat)
            });
            logger.LogInformation("The message was sent with id: {SentMessageId}", sentMessage.Id);
        }

        #region Subscription commands
        private async Task<Message> Subscribe(User user, Chat chat, IUserRepository userRepository)
        {
            try
            {
                await userRepository.SubscribeAsync(user.Id);
                logger.LogInformation("Subscribed user: {UserId} ({UserFirstName})", user.Id, user.FirstName);
                return await bot.SendMessage(chat, "You have subscribed!");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to subscribe user: {UserId} ({UserFirstName})", user.Id, user.FirstName);
                return await bot.SendMessage(chat, "Something went wrong. Try again later.");
            }
        }

        private async Task<Message> Unsubscribe(User user, Chat chat, IUserRepository userRepository)
        {
            try
            {
                await userRepository.UnsubscribeAsync(user.Id);
                logger.LogInformation("Unsubscribed user: {UserId} ({UserFirstName})", user.Id, user.FirstName);
                return await bot.SendMessage(chat, "You have unsubscribed!");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to unsubscribe user: {UserId} ({UserFirstName})", user.Id, user.FirstName);
                return await bot.SendMessage(chat, "Something went wrong. Try again later.");
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

        private async Task<Message> Send(Chat chat, string messageText, IUserRepository userRepository)
        {
            var parts = messageText.Split(' ', StringSplitOptions.TrimEntries);
            if (parts.Length == 2 && parts[1] == "start")
            {
                if (sender.Active)
                {
                    return await bot.SendMessage(chat, "Already in progress.");
                }

                int usersToProcess = await userRepository.GetQueryable().CountAsync(u => u.IsActive);
                return await bot.SendMessage(
                    chatId: chat,
                    text: $"The report will be sent to {usersToProcess} users. Proceed?",
                    replyMarkup: new InlineKeyboardButton[][]
                    {
                        [("Yes", "send start"), ("No", "cancel")]
                    });
            }
            else if (parts.Length == 2 && parts[1] == "stop")
            {
                if (!sender.Active)
                {
                    return await bot.SendMessage(chat, "Already stopped.");
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

        private async Task<Message> Stats(Chat chat, IUserRepository userRepository)
        {
            var overallStats = await userRepository.GetStatsAsync();
            var now = DateTime.UtcNow;
            var lastStats = await userRepository.GetStatsAsync(now.AddDays(-30), now);
            string text = $"""
                Total: {overallStats.ActiveUsers}/{overallStats.TotalUsers}
                Last 30 days: {lastStats.ActiveUsers}/{lastStats.TotalUsers}
                """;

            return await bot.SendMessage(chat, text);
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
            var from = DateTime.UtcNow.Date;
            var to = from.AddDays(_reportSettings.DaysToCover - 1);

            await bot.SendMessage(
                chatId: chat,
                text: "Start sending...",
                cancellationToken: cancellationToken);
            _sendingCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _sendingTask = Task.Run(async () =>
            {
                var result = await sender.SendReportAsync(from, to, _sendingCancellationTokenSource.Token);
                await bot.SendMessage(
                    chatId: chat,
                    text: $"<pre>{result}</pre>",
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken);
            });
        }

        private async Task StopSendingAsync(Chat chat)
        {
            _sendingCancellationTokenSource?.Cancel();
            await bot.SendMessage(chat, "Stopped.");
        }
        #endregion

        private async Task<Message> Usage(Chat chat)
        {
            const string usage = "Welcome! To start receiving EOL reports, type /subscribe. You can cancel your subscription at any time.";
            return await bot.SendMessage(chat, usage);
        }

        private Task UnknownUpdateHandlerAsync(Update update)
        {
            logger.LogInformation("Unknown update type: {UpdateType}", update.Type);
            return Task.CompletedTask;
        }
    }
}
