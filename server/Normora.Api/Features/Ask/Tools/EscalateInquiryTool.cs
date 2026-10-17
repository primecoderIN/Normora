using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Domain;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Interfaces;
using Normora.Shared.Templates;

namespace Normora.Api.Features.Ask.Tools;

/// <summary>
/// Tool that escalates an employee inquiry to workspace HR administrators via transactional email.
/// </summary>
public sealed class EscalateInquiryTool(
    TenantsDbContext tenantsContext,
    IEmailService emailService) : IAgentTool
{
    public string Name => "escalate_hr_inquiry";

    public string Description =>
        "Escalates an unanswered or complex employee inquiry to the workspace HR/People administrators by dispatching an email notification when company policy documents do not have the answer or human review is needed.";

    public object ParameterSchema => new
    {
        type = "object",
        properties = new
        {
            inquiry = new
            {
                type = "string",
                description = "The employee's original question or specific policy situation that requires HR clarification."
            },
            category = new
            {
                type = "string",
                description = "Departmental category: 'benefits', 'compensation', 'leave_of_absence', 'payroll', 'grievance', or 'general'."
            },
            urgency = new
            {
                type = "string",
                description = "Estimated urgency: 'low', 'medium', or 'high'."
            },
            notes = new
            {
                type = "string",
                description = "Any additional facts or employee context to assist the HR team."
            }
        },
        required = new[] { "inquiry" }
    };

    public async Task<AgentToolResult> ExecuteAsync(
        string argumentsJson,
        ToolExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(argumentsJson) ? "{}" : argumentsJson);
            var root = doc.RootElement;

            var inquiry = root.TryGetProperty("inquiry", out var inq) ? inq.GetString() : null;
            if (string.IsNullOrWhiteSpace(inquiry))
            {
                return new AgentToolResult(false, string.Empty, "The 'inquiry' field is required to escalate to HR.");
            }

            var category = root.TryGetProperty("category", out var cat) ? cat.GetString() : "general";
            var urgency = root.TryGetProperty("urgency", out var urg) ? urg.GetString() : "medium";
            var notes = root.TryGetProperty("notes", out var nt) ? nt.GetString() : null;

            // Fetch workspace administrators
            var adminMembers = await tenantsContext.TenantMemberships
                .AsNoTracking()
                .Include(m => m.User)
                .Where(m => m.TenantId == context.TenantId && m.Role == TenantRole.Admin)
                .ToListAsync(cancellationToken);

            var adminEmails = adminMembers
                .Select(m => m.User.Email)
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var employeeName = !string.IsNullOrWhiteSpace(context.UserDisplayName)
                ? context.UserDisplayName
                : (!string.IsNullOrWhiteSpace(context.UserEmail) ? context.UserEmail : "Employee");

            var employeeEmail = !string.IsNullOrWhiteSpace(context.UserEmail)
                ? context.UserEmail
                : "no-reply@normora.internal";

            var subject = $"[HR Escalation] {category?.ToUpperInvariant()}: Inquiry from {employeeName}";
            var additionalContext = string.IsNullOrWhiteSpace(notes)
                ? $"Urgency: {urgency}. Category: {category}."
                : $"Urgency: {urgency}. Category: {category}. Additional Notes: {notes}";

            var emailBody = EmailTemplates.EscalationInquiryEmail(
                context.TenantName,
                employeeName,
                employeeEmail,
                inquiry,
                additionalContext);

            var notifiedCount = 0;
            foreach (var email in adminEmails)
            {
                if (email != null)
                {
                    await emailService.SendEmailAsync(email, subject, emailBody, cancellationToken);
                    notifiedCount++;
                }
            }

            var message = notifiedCount > 0
                ? $"Your inquiry has been successfully escalated to {notifiedCount} HR administrator(s) at {context.TenantName}. An HR team member will follow up with you at {employeeEmail}."
                : $"Your inquiry has been logged for escalation at {context.TenantName}. The HR team has been notified.";

            var responseObj = new
            {
                status = "escalated",
                administratorsNotified = notifiedCount,
                inquiry,
                category,
                urgency,
                message
            };

            return new AgentToolResult(true, JsonSerializer.Serialize(responseObj, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            return new AgentToolResult(false, string.Empty, $"Failed to escalate inquiry: {ex.Message}");
        }
    }
}
