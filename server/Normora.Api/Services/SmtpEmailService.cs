using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Normora.Shared.Interfaces;
using Normora.Shared.Options;

namespace Normora.Api.Services;

/// <summary>
/// A concrete implementation of IEmailService that sends emails via an SMTP server.
/// Currently configured to connect to the local MailHog instance (port 1025) for development.
/// </summary>
public class SmtpEmailService : IEmailService, IDisposable
{
    private readonly SmtpClient _client;
    private readonly ILogger<SmtpEmailService> _logger;
    private readonly string _fromAddress;

    public SmtpEmailService(IOptions<SmtpOptions> options, ILogger<SmtpEmailService> logger)
    {
        _logger = logger;
        
        var smtpOptions = options.Value;
        var host = smtpOptions.Host;
        var port = smtpOptions.Port;
        _fromAddress = smtpOptions.From;

        _client = new SmtpClient(host, port)
        {
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false
        };
    }

    /// <inheritdoc />
    public async Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        try
        {
            // Construct and fire off an HTML-formatted email using our local MailHog server or production SMTP provider
            var message = new MailMessage(_fromAddress, to)
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            await _client.SendMailAsync(message, cancellationToken);
            _logger.LogInformation("Successfully sent email to {To} with subject {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To} with subject {Subject}", to, subject);
            throw; // Re-throw to allow upstream systems (like MediatR pipelines) to handle the failure.
        }
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}
