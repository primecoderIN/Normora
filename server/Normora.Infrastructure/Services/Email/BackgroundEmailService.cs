using Hangfire;
using Microsoft.Extensions.Logging;
using Normora.Infrastructure.Features.Email;
using Normora.Shared.Interfaces;

namespace Normora.Infrastructure.Services.Email;

/// <summary>
/// Asynchronous implementation of <see cref="IEmailService"/> that enqueues email delivery
/// tasks into Hangfire rather than blocking HTTP request threads.
/// </summary>
public class BackgroundEmailService(
    IBackgroundJobClient backgroundJobs,
    ILogger<BackgroundEmailService> logger) : IEmailService
{
    public Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Enqueuing background email job with subject '{Subject}'", subject);

        backgroundJobs.Enqueue<SendEmailJob>(job => job.ExecuteAsync(to, subject, body));

        return Task.CompletedTask;
    }
}
