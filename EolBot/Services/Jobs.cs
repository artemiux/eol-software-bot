using EolBot.Services.Report;
using EolBot.Services.Telegram;
using Microsoft.Extensions.Options;

namespace EolBot.Services
{
    class Jobs(IServiceProvider serviceProvider)
    {
        public async Task SendWeeklyReportAsync()
        {
            var options = serviceProvider.GetRequiredService<IOptions<ReportSettings>>();
            var sender = serviceProvider.GetRequiredService<TelegramSender>();
            var lifetime = serviceProvider.GetRequiredService<IHostApplicationLifetime>();

            var from = DateTime.UtcNow.Date;
            var to = from.AddDays(options.Value.DaysToCover - 1);
            _ = await sender.SendReportAsync(
                fromInclusive: from, toInclusive: to,
                stoppingToken: lifetime.ApplicationStopping);
        }
    }
}
