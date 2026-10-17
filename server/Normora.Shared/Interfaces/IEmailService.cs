namespace Normora.Shared.Interfaces;

/// <summary>
/// An abstraction for sending emails to users.
/// Enqueues emails into background queues (Hangfire) for reliable, non-blocking delivery.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Transmits an email to the specified recipient.
    /// </summary>
    /// <param name="to">The destination email address.</param>
    /// <param name="subject">The subject line of the email.</param>
    /// <param name="body">The HTML or plain text body of the email.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
}
