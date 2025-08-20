using EolBot.Models;
using EolBot.Repositories;
using EolBot.Repositories.Abstract;
using EolBot.Services.Report;
using EolBot.Services.Report.Abstract;
using EolBot.Services.Report.Provider.Abstract;
using Microsoft.Extensions.Options;
using System.Net;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;

namespace EolBot.Services.Telegram
{
    public class TelegramSender(
        IOptions<ReportSettings> reportOptions,
        IReportDataProvider provider,
        IReport report,
        IServiceScopeFactory scopeFactory,
        ITelegramBotClient botClient,
        ILogger<TelegramSender> logger)
    {
        public bool Active { get; private set; }

        private readonly SemaphoreSlim _lock = new(1, 1);
        private readonly ParallelOptions _parallelOptions = new()
        {
            // https://core.telegram.org/bots/faq#my-bot-is-hitting-limits-how-do-i-avoid-this
            MaxDegreeOfParallelism = reportOptions.Value.MaxConcurrentMessages
        };
        private readonly int _databaseRequestLimit = reportOptions.Value.MaxConcurrentMessages * 10;

        public async Task<SendingResult> SendReportAsync(DateTime fromInclusive, DateTime toInclusive,
            CancellationToken stoppingToken = default)
        {
            lock (this)
            {
                if (Active)
                {
                    return new SendingResult(Error: "ConcurrentError", ErrorMessage: "Process already active.");
                }
                Active = true;
            }

            string text;
            try
            {
                text = report.Create(fromInclusive, toInclusive, items: provider.Get(fromInclusive, toInclusive));
                logger.LogInformation("Report created:\n{Text}", text);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create report");
                Active = false;
                return new SendingResult(Error: "ReportError", ErrorMessage: ex.Message);
            }

            using var scope = scopeFactory.CreateScope();
            var reportRepository = scope.ServiceProvider.GetRequiredService<IReportRepository>();
            try
            {
                await reportRepository.AddAsync(text);
            }
            catch (Exception ex)
            {
                return new SendingResult
                (
                    Error: "DatabaseError",
                    ErrorMessage: ex.Message
                );
            }

            var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var sent = 0;
            var failed = 0;
            var start = 1;
            while (!stoppingToken.IsCancellationRequested && start > 0)
            {
                PaginatedResult<User> result;
                try
                {
                    result = await userRepository.GetAsync(
                        filter: u => u.IsActive,
                        start: start,
                        limit: _databaseRequestLimit);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to get users");
                    Active = false;
                    return new SendingResult
                    (
                        ReportRecipientsCount: sent,
                        Error: "DatabaseError",
                        ErrorMessage: ex.Message
                    );
                }
                start = result.Next.GetValueOrDefault();

                await Parallel.ForEachAsync(result.Result, _parallelOptions, async (user, ct) =>
                {
                    if (await SendMessage(user.TelegramId, text, userRepository))
                    {
                        Interlocked.Increment(ref sent);
                    }
                    else
                    {
                        Interlocked.Increment(ref failed);
                    }
                });
            }

            logger.LogInformation("Report sent to {Counter} users", sent);
            Active = false;

            if (stoppingToken.IsCancellationRequested)
            {
                return new SendingResult
                (
                    ReportRecipientsCount: sent,
                    Error: "Cancelled"
                );
            }

            return new SendingResult
            (
                Ok: failed == 0,
                ReportRecipientsCount: sent,
                Error: failed > 0 ? "DeliveryWarning" : null,
                ErrorMessage: failed > 0 ? $"There were {failed} failed recipients." : null
            );
        }

        private async Task<bool> SendMessage(long chatId, string text,
            IUserRepository userRepository)
        {
            var sent = false;
            try
            {
                await botClient.SendMessage(chatId, text, ParseMode.Html);
                logger.LogInformation("Message sent to user {ChatId}", chatId);
                sent = true;
            }
            catch (ApiRequestException ex)
                when (ex.HttpStatusCode == HttpStatusCode.Forbidden)
            {
                await _lock.WaitAsync();
                try
                {
                    await userRepository.UnsubscribeAsync(chatId);
                    logger.LogInformation("User {ChatId} blocked the bot and was unsubscribed", chatId);
                }
                catch (Exception innerEx)
                {
                    logger.LogWarning("Failed to unsubscribe user {ChatId}: {Message}",
                        chatId, innerEx.Message);
                }
                finally
                {
                    _lock.Release();
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning("Failed to send message to {ChatId}: {Message}", chatId, ex.Message);
            }

            return sent;
        }
    }

    public record SendingResult(
        bool Ok = false,
        int? ReportRecipientsCount = 0,
        string? Error = null, string? ErrorMessage = null);
}
