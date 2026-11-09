using Microsoft.Extensions.Logging;
using Normora.Shared.Interfaces;

namespace Normora.Infrastructure.Services.Email;

/// <summary>
/// A no-op implementation of <see cref="IEmailSender"/> used in testing or when email delivery is disabled.
/// Logs email actions without performing network transmission.
/// </summary>
public class NullEmailSender(ILogger<NullEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("[NullEmailSender] Suppressed email to '{Recipient}' with subject '{Subject}' (Body length: {Length} characters)",
            to, subject, htmlBody.Length);
        return Task.CompletedTask;
    }
}
