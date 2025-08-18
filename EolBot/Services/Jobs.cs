using EolBot.Services.Report;
using EolBot.Services.Telegram;
using Hangfire;
using Microsoft.Extensions.Options;
using Telegram.Bot;

namespace EolBot.Services
{
    class Jobs(IServiceProvider serviceProvider)
    {
        public async Task SendWeeklyReportAsync(IJobCancellationToken jobToken)
        {
            var lifetime = serviceProvider.GetRequiredService<IHostApplicationLifetime>();
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(jobToken.ShutdownToken, lifetime.ApplicationStopping);

            var reportOptions = serviceProvider.GetRequiredService<IOptions<ReportSettings>>();
            var from = DateTime.UtcNow.Date;
            var to = from.AddDays(reportOptions.Value.DaysToCover - 1);
            var sender = serviceProvider.GetRequiredService<TelegramSender>();
            var result = await sender.SendReportAsync(
                fromInclusive: from, toInclusive: to,
                stoppingToken: linkedCts.Token);

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
                serviceProvider.GetRequiredService<ILogger<Jobs>>().LogError(ex, "Failed to notify admin");
            }
        }
    }
}
