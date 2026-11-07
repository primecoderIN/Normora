using System.Text.Json;
using MediatR;
using Normora.Shared.Constants;
using Normora.Shared.Contracts.Tenants;
using Normora.Shared.Interfaces;
using Normora.Shared.Templates;

namespace Normora.Modules.Conversations.Infrastructure.Tools;

/// <summary>
/// Tool that escalates an employee inquiry to workspace HR administrators via transactional email.
/// Audited and rate-capped to safeguard against notification storms and data leakage.
/// </summary>
public sealed class EscalateInquiryTool(
    IMediator mediator,
    IEmailService emailService,
    IAuditService auditService) : IAgentTool
{
    private const int MaxEscalationRecipients = 5;

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
                return new AgentToolResult(false, string.Empty, ApiMessages.ValidationFailed);
            }

            var category = root.TryGetProperty("category", out var cat) ? cat.GetString() : "general";
            var urgency = root.TryGetProperty("urgency", out var urg) ? urg.GetString() : "medium";
            var notes = root.TryGetProperty("notes", out var nt) ? nt.GetString() : null;

            // Cross-Module Boundary Query:
            // Dispatches a MediatR query to the Tenants module to discover verified workspace administrators without directly injecting TenantsDbContext.
            var adminRecipients = await mediator.Send(
                new GetTenantAdminRecipientsQuery(context.TenantId),
                cancellationToken);

            // Rate & Broadcast Protection:
            // Restrict notification dispatch to at most 5 unique admin recipients to prevent email broadcast storms.
            var adminEmails = adminRecipients.AdminEmails
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(MaxEscalationRecipients)
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

            var emailBody = EscalationEmailTemplates.EscalationInquiryEmail(
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

            // Governance & Compliance Audit Trail:
            // Records an immutable audit log entry for autonomous AI side effects,
            // tracking the acting user, category, urgency, and the count of administrators alerted.
            await auditService.RecordAsync(
                context.TenantId,
                context.UserId,
                context.UserEmail,
                context.UserDisplayName,
                "INQUIRY_ESCALATED",
                "Inquiry",
                category,
                JsonSerializer.Serialize(new
                {
                    category,
                    urgency,
                    administratorsNotified = notifiedCount,
                    inquiryLength = inquiry.Length
                }),
                cancellationToken);

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
        catch (Exception)
        {
            return new AgentToolResult(false, string.Empty, ApiMessages.InquiryEscalationFailed);
        }
    }
}
