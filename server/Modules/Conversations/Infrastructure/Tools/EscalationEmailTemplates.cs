using System.Net;
using Normora.Shared.Templates;

namespace Normora.Modules.Conversations.Infrastructure.Tools;

/// <summary>
/// Email templates for conversation escalation tooling.
/// </summary>
public static class EscalationEmailTemplates
{
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
            <h2 style=""margin: 0 0 16px 0; font-size: 20px; font-weight: 600; color: {EmailLayout.TextColor};"">
                New employee inquiry in {safeTenant}
            </h2>
            <p style=""margin: 0 0 16px 0; font-size: 15px; line-height: 24px; color: {EmailLayout.TextColor};"">
                An employee requested policy assistance that could not be fully answered by automated knowledge base documentation:
            </p>
            <table style=""width: 100%; border-collapse: collapse; margin: 16px 0; font-size: 14px;"">
                <tr>
                    <td style=""padding: 8px 0; color: {EmailLayout.MutedTextColor}; width: 120px;"">Employee:</td>
                    <td style=""padding: 8px 0; color: {EmailLayout.TextColor}; font-weight: 600;"">{safeName} ({safeEmail})</td>
                </tr>
            </table>
            <div style=""background-color: #f8fafc; border: 1px solid {EmailLayout.BorderColor}; border-radius: 8px; padding: 16px; margin: 20px 0;"">
                <div style=""font-size: 12px; font-weight: 600; text-transform: uppercase; color: {EmailLayout.MutedTextColor}; margin-bottom: 6px;"">
                    Employee Inquiry
                </div>
                <div style=""font-size: 14px; line-height: 22px; color: {EmailLayout.TextColor}; word-break: break-word;"">
                    &ldquo;{safeQuestion}&rdquo;
                </div>
                {(safeContext != null ? $@"
                <div style=""font-size: 12px; font-weight: 600; text-transform: uppercase; color: {EmailLayout.MutedTextColor}; margin-top: 14px; margin-bottom: 4px;"">
                    Additional Context / Note
                </div>
                <div style=""font-size: 13px; color: {EmailLayout.MutedTextColor}; line-height: 20px;"">
                    {safeContext}
                </div>" : "")}
            </div>
            <p style=""margin: 16px 0 0 0; font-size: 14px; line-height: 22px; color: {EmailLayout.TextColor};"">
                Please follow up with <strong>{safeName}</strong> at <a href=""mailto:{safeEmail}"" style=""color: {EmailLayout.BrandPrimaryColor};"">{safeEmail}</a>.
            </p>";

        return EmailLayout.Wrap($"Inquiry Escalation: {safeTenant}", content);
    }
}

