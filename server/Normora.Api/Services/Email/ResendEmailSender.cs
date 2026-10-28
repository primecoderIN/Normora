using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Normora.Shared.Constants;
using Normora.Shared.Interfaces;
using Normora.Shared.Options;

namespace Normora.Api.Services.Email;

/// <summary>
/// Cloud transactional email implementation of <see cref="IEmailSender"/> powered by Resend (https://resend.com).
/// </summary>
public class ResendEmailSender(
    HttpClient httpClient,
    IOptions<EmailOptions> options,
    ILogger<ResendEmailSender> logger) : IEmailSender
{
    private const string ResendApiUrl = "https://api.resend.com/emails";

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var emailOptions = options.Value;
        if (string.IsNullOrWhiteSpace(emailOptions.ApiKey))
        {
            throw new InvalidOperationException(ApiMessages.EmailServiceNotConfigured);
        }

        var formattedFrom = !string.IsNullOrWhiteSpace(emailOptions.FromName)
            ? $"{emailOptions.FromName} <{emailOptions.FromEmail}>"
            : emailOptions.FromEmail;

        var targetRecipient = !string.IsNullOrWhiteSpace(emailOptions.DevOverrideRecipient)
            ? emailOptions.DevOverrideRecipient
            : to;

        var targetSubject = !string.IsNullOrWhiteSpace(emailOptions.DevOverrideRecipient) && !string.Equals(emailOptions.DevOverrideRecipient, to, StringComparison.OrdinalIgnoreCase)
            ? $"[Dev: to {to}] {subject}"
            : subject;

        var payload = new
        {
            from = formattedFrom,
            to = new[] { targetRecipient },
            subject = targetSubject,
            html = htmlBody
        };

        var json = JsonSerializer.Serialize(payload);
        using var request = new HttpRequestMessage(HttpMethod.Post, ResendApiUrl)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", emailOptions.ApiKey);

        try
        {
            var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogError("Resend API request failed with status {StatusCode}: {Response}", response.StatusCode, responseContent);
                response.EnsureSuccessStatusCode();
            }

            logger.LogInformation("Resend successfully dispatched email with subject '{Subject}'", subject);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to transmit email via Resend");
            throw;
        }
    }
}
