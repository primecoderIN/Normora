namespace Normora.Shared.Templates;

/// <summary>
/// Provides a responsive, branded HTML layout container for transactional system emails.
/// </summary>
public static class EmailLayout
{
    public const string BrandPrimaryColor = "#4f46e5"; // Indigo 600
    public const string BrandBgColor = "#f8fafc"; // Slate 50
    public const string CardBgColor = "#ffffff";
    public const string TextColor = "#1e293b"; // Slate 800
    public const string MutedTextColor = "#64748b"; // Slate 500
    public const string BorderColor = "#e2e8f0"; // Slate 200

    /// <summary>
    /// Wraps body content in a standard, responsive Normora container with header and footer.
    /// </summary>
    public static string Wrap(string preheader, string bodyHtml)
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

