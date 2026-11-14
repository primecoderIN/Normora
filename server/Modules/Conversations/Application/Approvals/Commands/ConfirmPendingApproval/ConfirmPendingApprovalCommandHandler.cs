using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Normora.Modules.Conversations.Application.Approvals.DTOs;
using Normora.Modules.Conversations.Domain;
using Normora.Modules.Conversations.Persistence;
using Normora.Shared.Constants;
using Normora.Shared.Exceptions;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Application.Approvals.Commands.ConfirmPendingApproval;

/// <summary>
/// Executes an authorized autonomous agent tool after human-in-the-loop confirmation.
/// </summary>
public sealed class ConfirmPendingApprovalCommandHandler(
    ConversationsDbContext context,
    IAgentToolRegistry toolRegistry,
    ICurrentUser currentUser,
    ILogger<ConfirmPendingApprovalCommandHandler> logger,
    IWorkspaceStateSynchronizer? stateSynchronizer = null)
    : IRequestHandler<ConfirmPendingApprovalCommand, ConfirmApprovalResultDto>
{
    public async Task<ConfirmApprovalResultDto> Handle(ConfirmPendingApprovalCommand request, CancellationToken cancellationToken)
    {
        // 1. Fetch pending approval with tenant filter
        var approval = await context.PendingApprovals
            .FirstOrDefaultAsync(p => p.Id == request.ApprovalId && p.TenantId == request.TenantId, cancellationToken);

        if (approval == null)
        {
            throw new BolaException();
        }

        // 2. Validate current state and expiration
        if (approval.Status != ApprovalStatus.Pending)
        {
            return new ConfirmApprovalResultDto(
                Success: false,
                ToolName: approval.ToolName,
                Output: null,
                ErrorMessage: ApiMessages.ApprovalAlreadyDecided,
                ChangedData: false,
                StructuredData: null);
        }

        if (approval.ExpiresAt < DateTimeOffset.UtcNow)
        {
            approval.Status = ApprovalStatus.Expired;
            await context.SaveChangesAsync(cancellationToken);

            return new ConfirmApprovalResultDto(
                Success: false,
                ToolName: approval.ToolName,
                Output: null,
                ErrorMessage: ApiMessages.ApprovalExpired,
                ChangedData: false,
                StructuredData: null);
        }

        // 3. Locate tool in registry
        var tool = toolRegistry.FindTool(approval.ToolName);
        if (tool == null)
        {
            logger.LogWarning("Approved tool '{ToolName}' not found in agent registry.", approval.ToolName);
            return new ConfirmApprovalResultDto(
                Success: false,
                ToolName: approval.ToolName,
                Output: null,
                ErrorMessage: $"Tool '{approval.ToolName}' is not registered in this workspace.",
                ChangedData: false,
                StructuredData: null);
        }

        // 4. Ensure confirmed flag is set to true for destructive actions
        var argsJson = approval.ArgumentsJson;
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(argsJson) ? "{}" : argsJson);
            if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                var dict = new Dictionary<string, object?>();
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    dict[prop.Name] = prop.Value.Clone();
                }
                dict["confirmed"] = true;
                argsJson = JsonSerializer.Serialize(dict);
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Failed to inject confirmed flag into arguments JSON.");
        }

        // 5. Construct execution context
        var toolContext = new ToolExecutionContext(
            approval.TenantId,
            "Workspace",
            currentUser.ExternalUserId ?? request.UserId.ToString(),
            currentUser.Email,
            currentUser.DisplayName,
            null,
            approval.ConversationId);

        // 6. Execute tool securely
        var result = await tool.ExecuteAsync(argsJson, toolContext, cancellationToken);

        // 7. Update approval status and persist
        if (result.Success)
        {
            approval.Status = ApprovalStatus.Approved;
            approval.ExecutedAt = DateTimeOffset.UtcNow;
            approval.ExecutionOutputJson = result.Output;
        }
        else
        {
            approval.ExecutionOutputJson = result.Error;
        }

        await context.SaveChangesAsync(cancellationToken);

        if (result.Success && result.ChangedData && stateSynchronizer != null)
        {
            await stateSynchronizer.IncrementVersionAsync(approval.TenantId, approval.ToolName, cancellationToken);
        }

        logger.LogInformation(
            "Pending approval {ApprovalId} for tool {ToolName} completed with success={Success}",
            approval.Id,
            approval.ToolName,
            result.Success);

        return new ConfirmApprovalResultDto(
            Success: result.Success,
            ToolName: approval.ToolName,
            Output: result.Output,
            ErrorMessage: result.Error,
            ChangedData: result.ChangedData,
            StructuredData: result.StructuredData);
    }
}

