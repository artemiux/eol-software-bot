using EolBot.Services.Report;
using EolBot.Services.Telegram;
using Hangfire;
using Microsoft.Extensions.Options;
using Telegram.Bot;

namespace EolBot.Services
{
    class Jobs(
        IServiceProvider serviceProvider,
        ILogger<Jobs> logger)
    {
        public async Task SendWeeklyReportAsync(IJobCancellationToken jobToken)
        {
            var reportOptions = serviceProvider.GetRequiredService<IOptions<ReportSettings>>();
            var from = DateTime.UtcNow.Date;
            var to = from.AddDays(reportOptions.Value.DaysToCover - 1);
            var sender = serviceProvider.GetRequiredService<TelegramSender>();
            var result = await sender.SendReportAsync(
                fromInclusive: from, toInclusive: to,
                stoppingToken: jobToken.ShutdownToken);

            var telegramOptions = serviceProvider.GetRequiredService<IOptions<TelegramSettings>>();
            using var scope = serviceProvider.CreateScope();
            var bot = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();
            try
            {
                await bot.SendMessage(
                    chatId: telegramOptions.Value.AdminChatId,
                    text: result.ToString());
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to notify admin");
            }
        }
    }
}
