using EolBot.Models;
using EolBot.Repositories;
using EolBot.Repositories.Abstract;
using EolBot.Services.Report;
using EolBot.Services.Report.Abstract;
using EolBot.Services.Report.Provider.Abstract;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;

namespace EolBot.Services.Telegram
{
    public partial class TelegramSender(
        IOptions<ReportSettings> reportOptions,
        IReportDataProvider provider,
        IReport reportService,
        IServiceScopeFactory scopeFactory,
        ITelegramBotClient botClient,
        ILogger<TelegramSender> logger)
    {
        public bool Active { get; private set; }

        private readonly SemaphoreSlim _lock = new(1, 1);
        private readonly int _databaseRequestLimit = reportOptions.Value.MaxConcurrentMessages * 10;

        public async Task<SendingResult> SendReportAsync(DateTime fromInclusive, DateTime toInclusive,
            CancellationToken stoppingToken = default)
        {
            _lock.Wait(stoppingToken);
            try
            {
                if (Active)
                {
                    return new SendingResult(Error: "ConcurrentError", ErrorMessage: "Process already active.");
                }
                Active = true;
            }
            finally
            {
                _lock.Release();
            }

            ConcurrentDictionary<string, string> reports = new();
            IEnumerable<ReportItem> items;
            try
            {
                items = await provider.GetAsync(fromInclusive, toInclusive, stoppingToken);
                string defaultReport = reportService.Create(fromInclusive, toInclusive, items);
                reports.TryAdd("default", defaultReport);
                LogReportCreated(logger, defaultReport);
            }
            catch (Exception ex)
            {
                LogReportFailed(logger, ex);
                Active = false;
                return new SendingResult(Error: "ReportError", ErrorMessage: ex.Message);
            }

            using var scope = scopeFactory.CreateScope();
            var reportRepository = scope.ServiceProvider.GetRequiredService<IReportRepository>();
            try
            {
                await reportRepository.AddAsync(fromInclusive, toInclusive, items);
            }
            catch (Exception ex)
            {
                return new SendingResult
                (
                    Error: "DatabaseError",
                    ErrorMessage: ex.Message
                );
            }

            ParallelOptions parallelOptions = new()
            {
                // https://core.telegram.org/bots/faq#my-bot-is-hitting-limits-how-do-i-avoid-this
                MaxDegreeOfParallelism = reportOptions.Value.MaxConcurrentMessages,
                CancellationToken = stoppingToken
            };

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
                    LogUsersFailed(logger, ex);
                    Active = false;
                    return new SendingResult
                    (
                        ReportRecipientsCount: sent,
                        Error: "DatabaseError",
                        ErrorMessage: ex.Message
                    );
                }
                start = result.Next.GetValueOrDefault();

                try
                {
                    await Parallel.ForEachAsync(result.Result, parallelOptions, async (user, ct) =>
                    {
                        string text = reports.GetOrAdd(
                            key: user.LanguageCode ?? "default",
                            valueFactory: (key) => reportService.Create(fromInclusive, toInclusive, items, key));
                        if (await SendMessage(user.TelegramId, text, userRepository, ct))
                        {
                            Interlocked.Increment(ref sent);
                        }
                        else
                        {
                            Interlocked.Increment(ref failed);
                        }
                    });

                    // Manage sending limits
                    await Task.Delay(1000, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            LogReportSent(logger, sent);
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
            IUserRepository userRepository, CancellationToken ct)
        {
            var sent = false;
            try
            {
                await botClient.SendMessage(chatId, text, ParseMode.Html, cancellationToken: ct);
                LogMessageSent(logger, chatId);
                sent = true;
            }
            catch (ApiRequestException ex)
                when (ex.ErrorCode == 403)
            {
                await _lock.WaitAsync();
                try
                {
                    await userRepository.UnsubscribeAsync(chatId);
                    LogForbidden(logger, chatId);
                }
                catch (Exception innerEx)
                {
                    LogUnsubscriptionFailed(logger, chatId, innerEx.Message);
                }
                finally
                {
                    _lock.Release();
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                LogMessageFailed(logger, chatId, ex.Message);
            }

            return sent;
        }

        #region Logging

        [LoggerMessage(LogLevel.Information, "Report created:\n{Text}")]
        static partial void LogReportCreated(ILogger logger, string text);

        [LoggerMessage(LogLevel.Error, "Failed to create report")]
        static partial void LogReportFailed(ILogger logger, Exception ex);

        [LoggerMessage(LogLevel.Error, "Failed to get users")]
        static partial void LogUsersFailed(ILogger logger, Exception ex);

        [LoggerMessage(LogLevel.Information, "Report sent to {Counter} users")]
        static partial void LogReportSent(ILogger logger, int counter);

        [LoggerMessage(LogLevel.Information, "Message sent to user {ChatId}")]
        static partial void LogMessageSent(ILogger logger, long chatId);

        [LoggerMessage(LogLevel.Information, "User {ChatId} blocked the bot and was unsubscribed")]
        static partial void LogForbidden(ILogger logger, long chatId);

        [LoggerMessage(LogLevel.Warning, "Failed to unsubscribe user {ChatId}: {Message}")]
        static partial void LogUnsubscriptionFailed(ILogger logger, long chatId, string message);

        [LoggerMessage(LogLevel.Warning, "Failed to send message to {ChatId}: {Message}")]
        static partial void LogMessageFailed(ILogger logger, long chatId, string message);

        #endregion
    }

    public sealed record SendingResult(
        bool Ok = false,
        int ReportRecipientsCount = 0,
        string? Error = null, string? ErrorMessage = null)
    {
        private bool PrintMembers(StringBuilder builder)
        {
            builder
                .Append(nameof(Ok))
                .Append(" = ")
                .Append(Ok)
                .Append(", ")
                .Append(nameof(ReportRecipientsCount))
                .Append(" = ")
                .Append(ReportRecipientsCount);

            if (Error is not null)
            {
                builder
                    .Append(", ")
                    .Append(nameof(Error))
                    .Append(@" = """)
                    .Append(Error)
                    .Append('"');
            }

            if (ErrorMessage is not null)
            {
                builder
                    .Append(", ")
                    .Append(nameof(ErrorMessage))
                    .Append(@" = """)
                    .Append(ErrorMessage)
                    .Append('"');
            }

            return true;
        }
    }
}
