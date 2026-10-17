namespace Normora.Shared.Interfaces;

/// <summary>
/// Transport abstraction for physically transmitting an email message to a recipient.
/// Concrete implementations (SMTP, SendGrid, Null) handle delivery protocol specifics.
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Transmits an email to the specified recipient.
    /// </summary>
    /// <param name="to">The destination email address.</param>
    /// <param name="subject">The subject line of the email.</param>
    /// <param name="htmlBody">The HTML-formatted body of the email.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default);
}
