using System.Net;
using Normora.Shared.Templates;

namespace Normora.Modules.Documents.Application.Jobs;

/// <summary>
/// Email templates for the Documents module processing jobs and alerts.
/// </summary>
public static class DocumentEmailTemplates
{
    /// <summary>
    /// Generates an alert email when document ingestion/processing fails.
    /// </summary>
    public static string DocumentProcessingFailedEmail(string tenantName, string fileName, string errorMessage)
    {
        var safeTenant = WebUtility.HtmlEncode(tenantName);
        var safeFile = WebUtility.HtmlEncode(fileName);
        var safeError = WebUtility.HtmlEncode(errorMessage);

        var content = $@"
            <div style=""display: inline-block; background-color: #fef2f2; border: 1px solid #fecaca; border-radius: 6px; padding: 6px 12px; margin-bottom: 16px;"">
                <span style=""color: #dc2626; font-size: 13px; font-weight: 600;"">Processing Alert</span>
            </div>
            <h2 style=""margin: 0 0 16px 0; font-size: 20px; font-weight: 600; color: {EmailLayout.TextColor};"">
                Document ingestion failed in {safeTenant}
            </h2>
            <p style=""margin: 0 0 16px 0; font-size: 15px; line-height: 24px; color: {EmailLayout.TextColor};"">
                The document <strong>{safeFile}</strong> could not be completely processed by Normora's ingestion pipeline.
            </p>
            <div style=""background-color: #f8fafc; border: 1px solid {EmailLayout.BorderColor}; border-radius: 8px; padding: 16px; margin: 20px 0;"">
                <div style=""font-size: 12px; font-weight: 600; text-transform: uppercase; color: {EmailLayout.MutedTextColor}; margin-bottom: 4px;"">
                    Error Details
                </div>
                <div style=""font-family: monospace; font-size: 13px; color: #dc2626; word-break: break-word;"">
                    {safeError}
                </div>
            </div>
            <p style=""margin: 16px 0 0 0; font-size: 14px; line-height: 22px; color: {EmailLayout.TextColor};"">
                <strong>Recommended action:</strong> Verify that the file is not password-protected or corrupted, and try uploading it again from your employer dashboard.
            </p>";

        return EmailLayout.Wrap($"Alert: Processing failed for {safeFile}", content);
    }

    /// <summary>
    /// Generates a notification email when a document has successfully completed processing and is ready for queries.
    /// </summary>
    public static string DocumentReadyEmail(string tenantName, string fileName, string? departmentNames = null)
    {
        var safeTenant = WebUtility.HtmlEncode(tenantName);
        var safeFile = WebUtility.HtmlEncode(fileName);
        var safeScope = !string.IsNullOrWhiteSpace(departmentNames) ? WebUtility.HtmlEncode(departmentNames) : "Company-Wide";

        var content = $@"
            <div style=""display: inline-block; background-color: #f0fdf4; border: 1px solid #bbf7d0; border-radius: 6px; padding: 6px 12px; margin-bottom: 16px;"">
                <span style=""color: #16a34a; font-size: 13px; font-weight: 600;"">Knowledge Base Update</span>
            </div>
            <h2 style=""margin: 0 0 16px 0; font-size: 20px; font-weight: 600; color: {EmailLayout.TextColor};"">
                New document ready in {safeTenant}
            </h2>
            <p style=""margin: 0 0 16px 0; font-size: 15px; line-height: 24px; color: {EmailLayout.TextColor};"">
                The document <strong>{safeFile}</strong> has been extracted, embedded, and is now queryable in Ask Normora.
            </p>
            <table style=""width: 100%; border-collapse: collapse; margin: 16px 0; font-size: 14px;"">
                <tr>
                    <td style=""padding: 8px 0; color: {EmailLayout.MutedTextColor}; width: 120px;"">Document:</td>
                    <td style=""padding: 8px 0; color: {EmailLayout.TextColor}; font-weight: 600;"">{safeFile}</td>
                </tr>
                <tr>
                    <td style=""padding: 8px 0; color: {EmailLayout.MutedTextColor};"">Target Scope:</td>
                    <td style=""padding: 8px 0; color: {EmailLayout.TextColor};"">{safeScope}</td>
                </tr>
            </table>
            <p style=""margin: 20px 0 0 0; font-size: 14px; line-height: 22px; color: {EmailLayout.TextColor};"">
                Employees with access to this scope can now ask questions and receive answers cited from this document.
            </p>";

        return EmailLayout.Wrap($"New document ready: {safeFile}", content);
    }
}

