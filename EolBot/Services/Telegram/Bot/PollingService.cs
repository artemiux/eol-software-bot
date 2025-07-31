using EolBot.Services.Telegram.Bot.Abstract;

namespace EolBot.Services.Telegram.Bot
{
    class PollingService(IServiceProvider serviceProvider, ILogger<PollingService> logger)
        : PollingServiceBase<ReceiverService>(serviceProvider, logger);
}
