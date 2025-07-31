namespace EolBot.Services.Telegram.Bot.Abstract
{
    abstract class PollingServiceBase<TReceiverService>(IServiceProvider serviceProvider, ILogger<PollingServiceBase<TReceiverService>> logger)
        : BackgroundService where TReceiverService : IReceiverService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Starting polling service");
            await DoWork(stoppingToken);
        }

        private async Task DoWork(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Create new IServiceScope on each iteration. This way we can leverage benefits
                    // of Scoped TReceiverService and typed HttpClient - we'll grab "fresh" instance each time.
                    using var scope = serviceProvider.CreateScope();
                    var receiver = scope.ServiceProvider.GetRequiredService<TReceiverService>();

                    await receiver.ReceiveAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError("Polling failed with exception: {Exception}", ex);
                    // Cooldown if something goes wrong.
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
        }
    }
}
