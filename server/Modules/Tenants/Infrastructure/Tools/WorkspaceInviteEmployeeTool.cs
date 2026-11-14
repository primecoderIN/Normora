using System.Text.Json;
using MediatR;
using Normora.Modules.Tenants.Application.Invitations.Commands;
using Normora.Shared.Constants;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Infrastructure.Tools;

/// <summary>
/// Autonomous agent tool that dispatches an official invitation email welcoming a new employee into the workspace organization.
/// </summary>
public sealed class WorkspaceInviteEmployeeTool(IMediator mediator) : IAgentTool
{
    public string Name => "workspace_invite_employee";

    public string Description =>
        "Dispatches an official email invitation to prospective employees welcoming them into the workspace organization.";

    public ToolCategory Category => ToolCategory.Mutation;

    public bool RequiresApproval => false;

    public ToolChipDescriptor ChipDescriptor => new(
        StartLabel: "Sending employee workspace invitation...",
        CompletedLabel: "Workspace invitation email dispatched",
        FailedLabel: "Failed to dispatch invitation email");

    public object ParameterSchema => new
    {
        type = "object",
        properties = new
        {
            email = new
            {
                type = "string",
                description = "The corporate email address of the prospective employee to invite."
            }
        },
        required = new[] { "email" }
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

            var email = root.TryGetProperty("email", out var e) ? e.GetString()?.Trim() : null;
            if (string.IsNullOrWhiteSpace(email))
            {
                return new AgentToolResult(false, string.Empty, ApiMessages.ValidationFailed);
            }

            var invitationId = await mediator.Send(new InviteEmployeeCommand(email), cancellationToken);

            var result = new
            {
                status = "sent",
                invitationId,
                email,
                organization = context.TenantName,
                expiresInHours = 48,
                message = $"An invitation email has been successfully dispatched to {email} to join {context.TenantName}."
            };

            var outputJson = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
            return new AgentToolResult(true, outputJson, StructuredData: result, ChangedData: true);
        }
        catch (Exception ex)
        {
            return new AgentToolResult(false, string.Empty, ex.Message ?? ApiMessages.InternalServerError);
        }
    }
}

