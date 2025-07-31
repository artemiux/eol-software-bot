using EolBot.Services.Telegram.Bot.Abstract;
using Telegram.Bot;

namespace EolBot.Services.Telegram.Bot
{
    class ReceiverService(ITelegramBotClient botClient, UpdateHandler updateHandler, ILogger<ReceiverServiceBase<UpdateHandler>> logger)
        : ReceiverServiceBase<UpdateHandler>(botClient, updateHandler, logger);
}
