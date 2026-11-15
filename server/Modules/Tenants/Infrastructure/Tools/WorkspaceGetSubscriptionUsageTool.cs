using System.Text.Json;
using MediatR;
using Normora.Modules.Tenants.Application.Subscription.Queries;
using Normora.Shared.Constants;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Infrastructure.Tools;

/// <summary>
/// Autonomous agent tool that retrieves subscription tier limits, quotas, and current resource usage.
/// </summary>
public sealed class WorkspaceGetSubscriptionUsageTool(IMediator mediator) : IAgentTool
{
    public string Name => "workspace_get_subscription_usage";

    public string Description =>
        "Retrieves workspace plan tier, seat and document quotas, storage consumption, and subscription status.";

    public ToolCategory Category => ToolCategory.ReadOnly;

    public bool RequiresApproval => false;

    public ToolChipDescriptor ChipDescriptor => new(
        StartLabel: "Checking workspace subscription quotas...",
        CompletedLabel: "Subscription usage retrieved",
        FailedLabel: "Failed to retrieve subscription usage");

    public object? ParameterSchema => null;

    public async Task<AgentToolResult> ExecuteAsync(
        string argumentsJson,
        ToolExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var usage = await mediator.Send(new GetTenantSubscriptionUsageQuery(), cancellationToken);

            var result = new
            {
                planTier = usage.PlanTier,
                documentsUsed = usage.CurrentDocuments,
                maxDocuments = usage.MaxDocuments,
                documentLimitReached = usage.IsDocumentLimitReached,
                employeesUsed = usage.CurrentEmployees,
                maxEmployees = usage.MaxEmployees,
                employeeLimitReached = usage.IsEmployeeLimitReached,
                storageBytesUsed = usage.CurrentStorageBytes,
                maxStorageBytes = usage.MaxStorageBytes,
                storageLimitReached = usage.IsStorageLimitReached,
                subscriptionStatus = usage.SubscriptionStatus ?? (usage.HasActiveSubscription ? "active" : "free"),
                validUntil = usage.ValidUntil,
                route = "/employer/settings/subscription"
            };

            var outputJson = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
            return new AgentToolResult(true, outputJson, StructuredData: result);
        }
        catch (Exception ex)
        {
            return new AgentToolResult(false, string.Empty, ex.Message ?? ApiMessages.InternalServerError);
        }
    }
}
