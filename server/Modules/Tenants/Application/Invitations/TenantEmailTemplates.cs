using System.Net;
using Normora.Shared.Templates;

namespace Normora.Modules.Tenants.Application.Invitations;

/// <summary>
/// Email templates for the Tenants module.
/// </summary>
public static class TenantEmailTemplates
{
    /// <summary>
    /// Generates an HTML email inviting an employee to join a workspace.
    /// </summary>
    public static string InvitationEmail(string tenantName, string acceptUrl, string? inviterName = null)
    {
        var safeTenant = WebUtility.HtmlEncode(tenantName);
        var safeInviter = inviterName != null ? WebUtility.HtmlEncode(inviterName) : "Your team";

        var content = $@"
            <h2 style=""margin: 0 0 16px 0; font-size: 20px; font-weight: 600; color: {EmailLayout.TextColor};"">
                You've been invited to join {safeTenant}
            </h2>
            <p style=""margin: 0 0 16px 0; font-size: 15px; line-height: 24px; color: {EmailLayout.TextColor};"">
                Hello! <strong>{safeInviter}</strong> has invited you to join the <strong>{safeTenant}</strong> workspace on Normora.
            </p>
            <p style=""margin: 0 0 24px 0; font-size: 15px; line-height: 24px; color: {EmailLayout.TextColor};"">
                Normora gives you fast, source-backed answers to questions regarding company policies, guidelines, and departmental documentation.
            </p>
            <div style=""text-align: center; margin: 32px 0;"">
                <a href=""{acceptUrl}"" target=""_blank"" style=""display: inline-block; background-color: {EmailLayout.BrandPrimaryColor}; color: #ffffff; text-decoration: none; padding: 12px 28px; border-radius: 8px; font-size: 15px; font-weight: 600; letter-spacing: 0.2px; box-shadow: 0 2px 4px rgba(79, 70, 229, 0.25);"">
                    Accept Invitation & Join
                </a>
            </div>
            <p style=""margin: 24px 0 0 0; font-size: 13px; line-height: 20px; color: {EmailLayout.MutedTextColor};"">
                If the button above doesn't work, copy and paste this link into your browser:<br />
                <a href=""{acceptUrl}"" style=""color: {EmailLayout.BrandPrimaryColor}; word-break: break-all;"">{acceptUrl}</a>
            </p>
            <p style=""margin: 16px 0 0 0; font-size: 12px; color: {EmailLayout.MutedTextColor};"">
                This invitation link will expire in 48 hours. If you were not expecting this invitation, you can safely ignore this email.
            </p>";

        return EmailLayout.Wrap($"Join {safeTenant} on Normora", content);
    }
}
