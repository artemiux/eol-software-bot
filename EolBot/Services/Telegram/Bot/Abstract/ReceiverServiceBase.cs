using Telegram.Bot;
using Telegram.Bot.Polling;

namespace EolBot.Services.Telegram.Bot.Abstract
{
    abstract class ReceiverServiceBase<TUpdateHandler>(ITelegramBotClient botClient, TUpdateHandler updateHandler, ILogger<ReceiverServiceBase<TUpdateHandler>> logger)
        : IReceiverService where TUpdateHandler : IUpdateHandler
    {
        public async Task ReceiveAsync(CancellationToken stoppingToken)
        {
            var receiverOptions = new ReceiverOptions() { DropPendingUpdates = true, AllowedUpdates = [] };

            var me = await botClient.GetMe(stoppingToken);
            logger.LogInformation("Start receiving updates for {BotName}", me.Username ?? me.FirstName);

            // Start receiving updates.
            await botClient.ReceiveAsync(updateHandler, receiverOptions, stoppingToken);
        }
    }
}
