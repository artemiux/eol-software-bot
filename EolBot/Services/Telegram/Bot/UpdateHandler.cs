using EolBot.Repositories.Abstract;
using EolBot.Services.Report;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
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
        ILogger<UpdateHandler> logger) : IUpdateHandler
    {
        private readonly TelegramSettings _telegramSettings = telegramOptions.Value;
        private readonly ReportSettings _reportSettings = reportOptions.Value;

        private CancellationTokenSource? _sendingCancellationTokenSource;
        private Task? _sendingTask;

        public async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
        {
            logger.LogInformation("HandleError: {Exception}", exception);
            // Cooldown in case of network connection error.
            if (exception is RequestException)
            {
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
            else if (_telegramSettings.AdminChatId != default)
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
                || msg.Chat.Type != ChatType.Private
                || msg.From?.IsBot == true)
                return;

            using var scope = scopeFactory.CreateScope();
            var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

            Message sentMessage = await (messageText.Split(' ', StringSplitOptions.TrimEntries)[0] switch
            {
                "/subscribe" => Subscribe(msg, userRepository),
                "/unsubscribe" => Unsubscribe(msg, userRepository),
                "/send" when msg.From!.Id == _telegramSettings.AdminChatId => Send(msg, userRepository),
                "/stats" when msg.From!.Id == _telegramSettings.AdminChatId => Stats(msg, userRepository),
                _ => Usage(msg)
            });
            logger.LogInformation("The message was sent with id: {SentMessageId}", sentMessage.Id);
        }

        #region Subscription commands
        private async Task<Message> Subscribe(Message msg, IUserRepository userRepository)
        {
            var userId = msg.From!.Id;
            try
            {
                await userRepository.SubscribeAsync(userId);
                logger.LogInformation("Subscribed user: {UserId}", userId);
                return await bot.SendMessage(userId, "You have subscribed!");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to subscribe user: {UserId}", userId);
                return await bot.SendMessage(userId, "Something went wrong. Try again later.");
            }
        }

        private async Task<Message> Unsubscribe(Message msg, IUserRepository userRepository)
        {
            var userId = msg.From!.Id;
            try
            {
                await userRepository.UnsubscribeAsync(userId);
                logger.LogInformation("Unsubscribed user: {UserId}", userId);
                return await bot.SendMessage(userId, "You have unsubscribed!");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to unsubscribe user: {UserId}", userId);
                return await bot.SendMessage(userId, "Something went wrong. Try again later.");
            }
        }
        #endregion

        #region Admin commands
        private async Task<Message> Send(Message msg, IUserRepository userRepository)
        {
            var userId = msg.From!.Id;

            var parts = msg.Text!.Split(' ', StringSplitOptions.TrimEntries);
            if (parts.Length == 2 && parts[1] == "start")
            {
                if (sender.Active)
                {
                    return await bot.SendMessage(userId, "Already in progress.");
                }

                int usersToProcess = await userRepository.GetQueryable().CountAsync(u => u.IsActive);
                return await bot.SendMessage(
                    chatId: userId,
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
                    return await bot.SendMessage(userId, "Already stopped.");
                }

                return await bot.SendMessage(
                    chatId: userId,
                    text: "Sending the report will be stopped. Proceed?",
                    replyMarkup: new InlineKeyboardButton[][]
                    {
                        [("Yes", "send stop"), ("No", "cancel")]
                    });
            }
            else
            {
                return await bot.SendMessage(
                    chatId: userId,
                    text: "Usage: /send <start|stop>");
            }
        }

        private async Task<Message> Stats(Message msg, IUserRepository userRepository)
        {
            var overallStats = await userRepository.GetStatsAsync();
            var now = DateTime.UtcNow;
            var lastStats = await userRepository.GetStatsAsync(now.AddDays(-30), now);
            string text = $"""
                Total: {overallStats.ActiveUsers}/{overallStats.TotalUsers}
                Last 30 days: {lastStats.ActiveUsers}/{lastStats.TotalUsers}
                """;

            return await bot.SendMessage(msg.From!.Id, text);
        }
        #endregion

        #region Callbacks
        private async Task OnCallbackQuery(CallbackQuery callbackQuery, CancellationToken cancellationToken)
        {
            await bot.DeleteMessage(
                chatId: callbackQuery.Message!.Chat,
                messageId: callbackQuery.Message.Id,
                cancellationToken: cancellationToken);

            switch (callbackQuery.Data)
            {
                case "send start":
                    await StartSendingAsync(callbackQuery, cancellationToken);
                    break;
                case "send stop":
                    await StopSendingAsync(callbackQuery);
                    break;
                default:
                    break;
            }
        }

        private async Task StartSendingAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken)
        {
            var from = DateTime.UtcNow.Date;
            var to = from.AddDays(_reportSettings.DaysToCover - 1);

            await bot.SendMessage(
                chatId: callbackQuery.Message!.Chat,
                text: "Start sending...",
                cancellationToken: cancellationToken);
            _sendingCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _sendingTask = Task.Run(async () =>
            {
                var counter = await sender.SendReportAsync(from, to, _sendingCancellationTokenSource.Token);
                await bot.SendMessage(
                    chatId: callbackQuery.Message!.Chat,
                    text: $"Done. Report sent to {counter} users.",
                    cancellationToken: cancellationToken);
            });
        }

        private async Task StopSendingAsync(CallbackQuery callbackQuery)
        {
            _sendingCancellationTokenSource?.Cancel();
            await bot.SendMessage(callbackQuery.Message!.Chat, "Stopped.");
        }
        #endregion

        private async Task<Message> Usage(Message msg)
        {
            const string usage = "Welcome! To start receiving EOL reports, type /subscribe. You can cancel your subscription at any time.";
            return await bot.SendMessage(msg.Chat, usage);
        }

        private Task UnknownUpdateHandlerAsync(Update update)
        {
            logger.LogInformation("Unknown update type: {UpdateType}", update.Type);
            return Task.CompletedTask;
        }
    }
}
