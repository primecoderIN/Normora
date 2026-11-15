using System.Text.Json;
using Normora.Shared.Constants;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Infrastructure.Tools;

/// <summary>
/// Autonomous agent tool that actuates client navigation to any workspace view or setting.
/// </summary>
public sealed class WorkspaceNavigateTool : IAgentTool
{
    public string Name => "workspace_navigate";

    public string Description =>
        "Navigates the user directly to any section of the Normora application (dashboard, documents, employees, departments, user_groups, branding, audit_logs, subscription, conversations, saved_answers, profile).";

    public ToolCategory Category => ToolCategory.ReadOnly;

    public bool RequiresApproval => false;

    public ToolChipDescriptor ChipDescriptor => new(
        StartLabel: "Preparing workspace navigation...",
        CompletedLabel: "Navigation ready",
        FailedLabel: "Navigation failed");

    public object? ParameterSchema => new
    {
        type = "object",
        properties = new
        {
            destination = new
            {
                type = "string",
                description = "The target page to navigate to: dashboard, documents, employees, departments, user_groups, branding, audit_logs, subscription, conversations, saved_answers, or profile.",
                @enum = new[]
                {
                    "dashboard", "documents", "employees", "departments",
                    "user_groups", "branding", "audit_logs", "subscription",
                    "conversations", "saved_answers", "profile"
                }
            }
        },
        required = new[] { "destination" }
    };

    public Task<AgentToolResult> ExecuteAsync(
        string argumentsJson,
        ToolExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var doc = JsonDocument.Parse(argumentsJson);
            var root = doc.RootElement;

            var destination = root.TryGetProperty("destination", out var dProp) ? dProp.GetString()?.ToLowerInvariant() : "dashboard";

            var (route, title, description) = destination switch
            {
                "documents" => ("/employer/documents", "Document Management", "Upload, manage, and configure departmental clearances for company knowledge files."),
                "employees" => ("/employer/employees", "Employee Directory", "Invite, offboard, and configure department and group assignments for team members."),
                "departments" => ("/employer/settings/departments", "Department Management", "Create, rename, and manage organizational departments and access boundaries."),
                "user_groups" => ("/employer/settings/user-groups", "User Security Groups", "Configure security groups, member rosters, and department clearances."),
                "branding" => ("/employer/settings/branding", "Branding & White-Label", "Customize workspace theme colors, logo assets, and vanity styling."),
                "audit_logs" => ("/employer/settings/audit-logs", "Audit Logs", "Review security, invitation, and administrative change logs."),
                "subscription" => ("/employer/settings/subscription", "Subscription & Billing", "Monitor seat quotas, document limits, storage usage, and plan tiers."),
                "conversations" => ("/employee/conversations", "AI Conversations", "Engage in multi-turn conversational RAG across corporate documents."),
                "saved_answers" => ("/employee/saved-answers", "Saved Policy Answers", "Review and export bookmarked AI answers and policy summaries."),
                "profile" => ("/employer/profile", "User Profile", "Manage your personal account settings, credentials, and tenant memberships."),
                _ => ("/employer/dashboard", "Workspace Dashboard", "View organization-wide health metrics, quick actions, and recent activity.")
            };

            var result = new
            {
                destination,
                title,
                description,
                route
            };

            var outputJson = JsonSerializer.Serialize(result);
            return Task.FromResult(new AgentToolResult(true, outputJson, StructuredData: result));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new AgentToolResult(false, string.Empty, ex.Message ?? ApiMessages.InternalServerError));
        }
    }
}
