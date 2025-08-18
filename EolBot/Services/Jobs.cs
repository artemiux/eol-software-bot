using EolBot.Services.Report;
using EolBot.Services.Telegram;
using Microsoft.Extensions.Options;
using Telegram.Bot;

namespace EolBot.Services
{
    class Jobs(IServiceProvider serviceProvider)
    {
        public async Task SendWeeklyReportAsync()
        {
            var reportOptions = serviceProvider.GetRequiredService<IOptions<ReportSettings>>();
            var sender = serviceProvider.GetRequiredService<TelegramSender>();
            var lifetime = serviceProvider.GetRequiredService<IHostApplicationLifetime>();

            var from = DateTime.UtcNow.Date;
            var to = from.AddDays(reportOptions.Value.DaysToCover - 1);
            var result = await sender.SendReportAsync(
                fromInclusive: from, toInclusive: to,
                stoppingToken: lifetime.ApplicationStopping);

            var telegramOptions = serviceProvider.GetRequiredService<IOptions<TelegramSettings>>();
            using var scope = serviceProvider.CreateScope();
            var bot = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();
            try
            {
                await bot.SendMessage(
                    chatId: telegramOptions.Value.AdminChatId,
                    text: result.ToString(),
                    cancellationToken: lifetime.ApplicationStopping);
            }
            catch (Exception ex)
            {
                serviceProvider.GetRequiredService<ILogger<Jobs>>().LogError(ex, "Failed to notify admin");
            }
        }
    }
}
