using System.Net;

namespace Normora.Shared.Templates;

/// <summary>
/// Provides responsive, branded HTML templates for transactional system emails.
/// </summary>
public static class EmailTemplates
{
    private const string BrandPrimaryColor = "#4f46e5"; // Indigo 600
    private const string BrandBgColor = "#f8fafc"; // Slate 50
    private const string CardBgColor = "#ffffff";
    private const string TextColor = "#1e293b"; // Slate 800
    private const string MutedTextColor = "#64748b"; // Slate 500
    private const string BorderColor = "#e2e8f0"; // Slate 200

    /// <summary>
    /// Generates an HTML email inviting an employee to join a workspace.
    /// </summary>
    public static string InvitationEmail(string tenantName, string acceptUrl, string? inviterName = null)
    {
        var safeTenant = WebUtility.HtmlEncode(tenantName);
        var safeInviter = inviterName != null ? WebUtility.HtmlEncode(inviterName) : "Your team";

        var content = $@"
            <h2 style=""margin: 0 0 16px 0; font-size: 20px; font-weight: 600; color: {TextColor};"">
                You've been invited to join {safeTenant}
            </h2>
            <p style=""margin: 0 0 16px 0; font-size: 15px; line-height: 24px; color: {TextColor};"">
                Hello! <strong>{safeInviter}</strong> has invited you to join the <strong>{safeTenant}</strong> workspace on Normora.
            </p>
            <p style=""margin: 0 0 24px 0; font-size: 15px; line-height: 24px; color: {TextColor};"">
                Normora gives you fast, source-backed answers to questions regarding company policies, guidelines, and departmental documentation.
            </p>
            <div style=""text-align: center; margin: 32px 0;"">
                <a href=""{acceptUrl}"" target=""_blank"" style=""display: inline-block; background-color: {BrandPrimaryColor}; color: #ffffff; text-decoration: none; padding: 12px 28px; border-radius: 8px; font-size: 15px; font-weight: 600; letter-spacing: 0.2px; box-shadow: 0 2px 4px rgba(79, 70, 229, 0.25);"">
                    Accept Invitation & Join
                </a>
            </div>
            <p style=""margin: 24px 0 0 0; font-size: 13px; line-height: 20px; color: {MutedTextColor};"">
                If the button above doesn't work, copy and paste this link into your browser:<br />
                <a href=""{acceptUrl}"" style=""color: {BrandPrimaryColor}; word-break: break-all;"">{acceptUrl}</a>
            </p>
            <p style=""margin: 16px 0 0 0; font-size: 12px; color: {MutedTextColor};"">
                This invitation link will expire in 48 hours. If you were not expecting this invitation, you can safely ignore this email.
            </p>";

        return WrapInLayout($"Join {safeTenant} on Normora", content);
    }

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
            <h2 style=""margin: 0 0 16px 0; font-size: 20px; font-weight: 600; color: {TextColor};"">
                Document ingestion failed in {safeTenant}
            </h2>
            <p style=""margin: 0 0 16px 0; font-size: 15px; line-height: 24px; color: {TextColor};"">
                The document <strong>{safeFile}</strong> could not be completely processed by Normora's ingestion pipeline.
            </p>
            <div style=""background-color: #f8fafc; border: 1px solid {BorderColor}; border-radius: 8px; padding: 16px; margin: 20px 0;"">
                <div style=""font-size: 12px; font-weight: 600; text-transform: uppercase; color: {MutedTextColor}; margin-bottom: 4px;"">
                    Error Details
                </div>
                <div style=""font-family: monospace; font-size: 13px; color: #dc2626; word-break: break-word;"">
                    {safeError}
                </div>
            </div>
            <p style=""margin: 16px 0 0 0; font-size: 14px; line-height: 22px; color: {TextColor};"">
                <strong>Recommended action:</strong> Verify that the file is not password-protected or corrupted, and try uploading it again from your employer dashboard.
            </p>";

        return WrapInLayout($"Alert: Processing failed for {safeFile}", content);
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
            <h2 style=""margin: 0 0 16px 0; font-size: 20px; font-weight: 600; color: {TextColor};"">
                New document ready in {safeTenant}
            </h2>
            <p style=""margin: 0 0 16px 0; font-size: 15px; line-height: 24px; color: {TextColor};"">
                The document <strong>{safeFile}</strong> has been extracted, embedded, and is now queryable in Ask Normora.
            </p>
            <table style=""width: 100%; border-collapse: collapse; margin: 16px 0; font-size: 14px;"">
                <tr>
                    <td style=""padding: 8px 0; color: {MutedTextColor}; width: 120px;"">Document:</td>
                    <td style=""padding: 8px 0; color: {TextColor}; font-weight: 600;"">{safeFile}</td>
                </tr>
                <tr>
                    <td style=""padding: 8px 0; color: {MutedTextColor};"">Target Scope:</td>
                    <td style=""padding: 8px 0; color: {TextColor};"">{safeScope}</td>
                </tr>
            </table>
            <p style=""margin: 20px 0 0 0; font-size: 14px; line-height: 22px; color: {TextColor};"">
                Employees with access to this scope can now ask questions and receive answers cited from this document.
            </p>";

        return WrapInLayout($"New document ready: {safeFile}", content);
    }

    /// <summary>
    /// Generates an email alerting tenant administrators of an escalated HR or policy inquiry.
    /// </summary>
    public static string EscalationInquiryEmail(
        string tenantName,
        string employeeName,
        string employeeEmail,
        string inquiryQuestion,
        string? additionalContext = null)
    {
        var safeTenant = WebUtility.HtmlEncode(tenantName);
        var safeName = WebUtility.HtmlEncode(employeeName);
        var safeEmail = WebUtility.HtmlEncode(employeeEmail);
        var safeQuestion = WebUtility.HtmlEncode(inquiryQuestion);
        var safeContext = !string.IsNullOrWhiteSpace(additionalContext) ? WebUtility.HtmlEncode(additionalContext) : null;

        var content = $@"
            <div style=""display: inline-block; background-color: #eff6ff; border: 1px solid #bfdbfe; border-radius: 6px; padding: 6px 12px; margin-bottom: 16px;"">
                <span style=""color: #2563eb; font-size: 13px; font-weight: 600;"">HR Policy Escalation</span>
            </div>
            <h2 style=""margin: 0 0 16px 0; font-size: 20px; font-weight: 600; color: {TextColor};"">
                New employee inquiry in {safeTenant}
            </h2>
            <p style=""margin: 0 0 16px 0; font-size: 15px; line-height: 24px; color: {TextColor};"">
                An employee requested policy assistance that could not be fully answered by automated knowledge base documentation:
            </p>
            <table style=""width: 100%; border-collapse: collapse; margin: 16px 0; font-size: 14px;"">
                <tr>
                    <td style=""padding: 8px 0; color: {MutedTextColor}; width: 120px;"">Employee:</td>
                    <td style=""padding: 8px 0; color: {TextColor}; font-weight: 600;"">{safeName} ({safeEmail})</td>
                </tr>
            </table>
            <div style=""background-color: #f8fafc; border: 1px solid {BorderColor}; border-radius: 8px; padding: 16px; margin: 20px 0;"">
                <div style=""font-size: 12px; font-weight: 600; text-transform: uppercase; color: {MutedTextColor}; margin-bottom: 6px;"">
                    Employee Inquiry
                </div>
                <div style=""font-size: 14px; line-height: 22px; color: {TextColor}; word-break: break-word;"">
                    &ldquo;{safeQuestion}&rdquo;
                </div>
                {(safeContext != null ? $@"
                <div style=""font-size: 12px; font-weight: 600; text-transform: uppercase; color: {MutedTextColor}; margin-top: 14px; margin-bottom: 4px;"">
                    Additional Context / Note
                </div>
                <div style=""font-size: 13px; color: {MutedTextColor}; line-height: 20px;"">
                    {safeContext}
                </div>" : "")}
            </div>
            <p style=""margin: 16px 0 0 0; font-size: 14px; line-height: 22px; color: {TextColor};"">
                Please follow up with <strong>{safeName}</strong> at <a href=""mailto:{safeEmail}"" style=""color: {BrandPrimaryColor};"">{safeEmail}</a>.
            </p>";

        return WrapInLayout($"Inquiry Escalation: {safeTenant}", content);
    }

    /// <summary>
    /// Wraps body content in a standard, responsive Normora container with header and footer.
    /// </summary>
    private static string WrapInLayout(string preheader, string bodyHtml)
    {
        return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
    <title>{preheader}</title>
</head>
<body style=""margin: 0; padding: 0; background-color: {BrandBgColor}; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; -webkit-font-smoothing: antialiased;"">
    <div style=""display: none; max-height: 0px; overflow: hidden;"">
        {preheader}
    </div>
    <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""background-color: {BrandBgColor}; padding: 40px 16px;"">
        <tr>
            <td align=""center"">
                <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""max-width: 580px;"">
                    <!-- Brand Header -->
                    <tr>
                        <td align=""center"" style=""padding-bottom: 24px;"">
                            <div style=""display: inline-flex; align-items: center; gap: 8px;"">
                                <span style=""font-size: 22px; font-weight: 800; letter-spacing: -0.5px; color: #0f172a;"">
                                    Norm<span style=""color: {BrandPrimaryColor};"">ora</span>
                                </span>
                            </div>
                        </td>
                    </tr>
                    <!-- Main Card -->
                    <tr>
                        <td style=""background-color: {CardBgColor}; border-radius: 12px; border: 1px solid {BorderColor}; padding: 36px 32px; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.05), 0 2px 4px -2px rgba(0, 0, 0, 0.05);"">
                            {bodyHtml}
                        </td>
                    </tr>
                    <!-- Footer -->
                    <tr>
                        <td align=""center"" style=""padding-top: 24px; font-size: 12px; line-height: 18px; color: {MutedTextColor};"">
                            <p style=""margin: 0 0 6px 0;"">
                                Sent by Normora Enterprise Knowledge Assistant
                            </p>
                            <p style=""margin: 0;"">
                                &copy; {DateTime.UtcNow.Year} Normora Inc. All rights reserved.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }
}
