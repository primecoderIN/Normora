using Hangfire;
using Microsoft.Extensions.Logging;
using Normora.Shared.Interfaces;

namespace Normora.Api.Features.Email;

/// <summary>
/// Hangfire background job responsible for asynchronously dispatching emails.
/// Decouples HTTP request lifecycles from physical SMTP/API network latency and provides automatic retry behavior.
/// </summary>
public class SendEmailJob(
    IEmailSender emailSender,
    ILogger<SendEmailJob> logger)
{
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = [10, 30, 60])]
    public async Task ExecuteAsync(string to, string subject, string htmlBody)
    {
        logger.LogInformation("Hangfire processing SendEmailJob with subject '{Subject}'", subject);

        await emailSender.SendAsync(to, subject, htmlBody);

        logger.LogInformation("Hangfire successfully completed SendEmailJob");
    }
}
