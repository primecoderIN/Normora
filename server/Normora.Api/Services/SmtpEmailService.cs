using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Normora.Shared.Interfaces;

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

    public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
    {
        _logger = logger;
        
        var host = configuration["Smtp:Host"] ?? "localhost";
        var portStr = configuration["Smtp:Port"] ?? "1025";
        var port = int.Parse(portStr);
        _fromAddress = configuration["Smtp:From"] ?? "noreply@normora.local";

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
