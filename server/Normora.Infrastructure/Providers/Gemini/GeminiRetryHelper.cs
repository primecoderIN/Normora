using System.Net;

namespace Normora.Infrastructure.Providers.Gemini;

/// <summary>
/// Provides resilient execution with exponential backoff and jitter for upstream Google Gemini API requests.
/// Automatically retries transient 429 (Too Many Requests) and 5xx server errors without failing immediately.
/// </summary>
internal static class GeminiRetryHelper
{
    private const int MaxRetries = 3;

    public static async Task<HttpResponseMessage> SendWithRetryAsync(
        HttpClient httpClient,
        Func<HttpRequestMessage> requestFactory,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            var request = requestFactory();
            var response = await httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode != HttpStatusCode.TooManyRequests &&
                (int)response.StatusCode < 500)
            {
                return response;
            }

            if (attempt > MaxRetries)
            {
                return response;
            }

            response.Dispose();

            // Exponential backoff with random jitter:
            // Attempt 1: ~1.2s - 1.8s
            // Attempt 2: ~2.4s - 3.2s
            // Attempt 3: ~4.8s - 6.0s
            var baseDelay = (int)Math.Pow(2, attempt) * 600;
            var jitter = Random.Shared.Next(200, 600);
            var delayMs = baseDelay + jitter;

            await Task.Delay(delayMs, cancellationToken);
        }
    }

    public static async Task<HttpResponseMessage> SendStreamingWithRetryAsync(
        HttpClient httpClient,
        Func<HttpRequestMessage> requestFactory,
        HttpCompletionOption completionOption,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            var request = requestFactory();
            var response = await httpClient.SendAsync(request, completionOption, cancellationToken);

            if (response.StatusCode != HttpStatusCode.TooManyRequests &&
                (int)response.StatusCode < 500)
            {
                return response;
            }

            if (attempt > MaxRetries)
            {
                return response;
            }

            response.Dispose();

            var baseDelay = (int)Math.Pow(2, attempt) * 600;
            var jitter = Random.Shared.Next(200, 600);
            var delayMs = baseDelay + jitter;

            await Task.Delay(delayMs, cancellationToken);
        }
    }
}

