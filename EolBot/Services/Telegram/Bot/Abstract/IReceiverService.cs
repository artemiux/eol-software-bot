namespace EolBot.Services.Telegram.Bot.Abstract
{
    interface IReceiverService
    {
        Task ReceiveAsync(CancellationToken stoppingToken);
    }
}
