using System.Text.Json;
using MediatR;
using Normora.Modules.Tenants.Application.Users.Queries;
using Normora.Shared.Constants;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Infrastructure.Tools;

/// <summary>
/// Autonomous agent tool that retrieves the authenticated user's profile, roles, and workspace memberships.
/// </summary>
public sealed class UserGetProfileTool(IMediator mediator) : IAgentTool
{
    public string Name => "user_get_profile";

    public string Description =>
        "Retrieves the currently authenticated user's profile details, assigned roles, and workspace memberships.";

    public ToolCategory Category => ToolCategory.ReadOnly;

    public bool RequiresApproval => false;

    public ToolChipDescriptor ChipDescriptor => new(
        StartLabel: "Retrieving user profile...",
        CompletedLabel: "User profile retrieved",
        FailedLabel: "Failed to retrieve user profile");

    public object? ParameterSchema => null;

    public async Task<AgentToolResult> ExecuteAsync(
        string argumentsJson,
        ToolExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await mediator.Send(new GetCurrentUserQuery(), cancellationToken);

            var activeMembership = user.Memberships.FirstOrDefault(m => m.TenantId == context.TenantId);

            var result = new
            {
                userId = user.Id,
                email = user.Email,
                displayName = user.DisplayName,
                currentRole = activeMembership?.Role ?? "Employee",
                currentWorkspace = activeMembership?.TenantName ?? context.TenantName,
                workspaceSlug = activeMembership?.TenantSlug,
                totalWorkspaces = user.Memberships.Count,
                pendingInvitations = user.PendingInvitations.Count,
                route = "/employer/profile"
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
