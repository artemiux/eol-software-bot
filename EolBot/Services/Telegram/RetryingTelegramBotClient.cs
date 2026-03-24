using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Requests.Abstractions;
using Telegram.Bot.Types;

namespace EolBot.Services.Telegram;

class RetryingTelegramBotClient(ITelegramBotClient backingClient, int maxRetries = 0, int jitter = 5)
    : ITelegramBotClient
{
    private readonly int _maxRetries = maxRetries >= 0
        ? maxRetries
        : throw new ArgumentException("Cannot be negative", nameof(maxRetries));

    private async Task<TResponse> SendRequestWithRetry<TResponse>(
        IRequest<TResponse> request, CancellationToken ct)
    {
        int retriesLeft = _maxRetries;
        while (true)
        {
            try
            {
                return await backingClient.SendRequest(request, ct);
            }
            catch (ApiRequestException ex) when (ex.ErrorCode == 429)
            {
                if (_maxRetries > 0 && --retriesLeft == 0)
                    throw new InvalidOperationException($"Exceeded maximum number of retries ({_maxRetries}).", ex);

                if (ex.Parameters?.RetryAfter is not int retryAfter)
                {
                    throw new InvalidOperationException("Rate limit exceeded but no retry-after value was provided.");
                }

                await Task.Delay(TimeSpan.FromSeconds(retryAfter + Random.Shared.Next(jitter)), ct);
            }
        }
    }

    public bool LocalBotServer => backingClient.LocalBotServer;

    public long BotId => backingClient.BotId;

    public TimeSpan Timeout { get => backingClient.Timeout; set => backingClient.Timeout = value; }

    public IExceptionParser ExceptionsParser { get => backingClient.ExceptionsParser; set => backingClient.ExceptionsParser = value; }

    public event AsyncEventHandler<ApiRequestEventArgs>? OnMakingApiRequest
    {
        add
        {
            backingClient.OnMakingApiRequest += value;
        }

        remove
        {
            backingClient.OnMakingApiRequest -= value;
        }
    }

    public event AsyncEventHandler<ApiResponseEventArgs>? OnApiResponseReceived
    {
        add
        {
            backingClient.OnApiResponseReceived += value;
        }

        remove
        {
            backingClient.OnApiResponseReceived -= value;
        }
    }

    public Task DownloadFile(string filePath, Stream destination, CancellationToken cancellationToken = default)
        => backingClient.DownloadFile(filePath, destination, cancellationToken);

    public Task DownloadFile(TGFile file, Stream destination, CancellationToken cancellationToken = default)
        => backingClient.DownloadFile(file, destination, cancellationToken);

    public Task<TResponse> SendRequest<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        => SendRequestWithRetry(request, cancellationToken);

    public Task<bool> TestApi(CancellationToken cancellationToken = default)
        => backingClient.TestApi(cancellationToken);
}
