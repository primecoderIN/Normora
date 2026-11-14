using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Normora.Modules.Conversations.Application.Approvals.Commands.ConfirmPendingApproval;
using Normora.Modules.Conversations.Application.Approvals.Commands.DismissPendingApproval;
using Normora.Modules.Conversations.Application.Approvals.DTOs;
using Normora.Modules.Conversations.Application.Approvals.Queries.GetPendingApprovals;
using Normora.Shared;
using Normora.Shared.Authorization;
using Normora.Shared.Constants;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.API;

/// <summary>
/// Provides endpoints for authorizing, executing, or dismissing human-in-the-loop (HITL) agent approvals.
/// </summary>
[ApiController]
[Route("api/conversations/approvals")]
[Authorize]
[RequireTenant(TenantRoles.Employee, TenantRoles.Admin)]
[Produces("application/json")]
public class ApprovalsController(
    IMediator mediator,
    ITenantContext tenantContext,
    ICurrentUser currentUser) : ControllerBase
{
    /// <summary>
    /// Confirms and immediately executes a pending autonomous agent action.
    /// </summary>
    [HttpPost("{approvalId:guid}/confirm")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<ConfirmApprovalResultDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ApiResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ApiResponse))]
    public async Task<IActionResult> Confirm(
        [FromRoute] Guid approvalId,
        CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;
        if (!tenantId.HasValue)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Failure(ApiMessages.Forbidden));
        }

        var userId = Guid.TryParse(currentUser.ExternalUserId, out var parsed) ? parsed : Guid.Empty;
        var result = await mediator.Send(new ConfirmPendingApprovalCommand(approvalId, tenantId.Value, userId), cancellationToken);

        if (!result.Success)
        {
            return BadRequest(ApiResponse<ConfirmApprovalResultDto>.Failure(result.ErrorMessage ?? ApiMessages.InternalServerError, result));
        }

        return Ok(ApiResponse<ConfirmApprovalResultDto>.Ok(result, ApiMessages.ActionApproved));
    }

    /// <summary>
    /// Dismisses/cancels a pending autonomous agent action without executing it.
    /// </summary>
    [HttpPost("{approvalId:guid}/dismiss")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<DismissApprovalResultDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ApiResponse))]
    public async Task<IActionResult> Dismiss(
        [FromRoute] Guid approvalId,
        CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;
        if (!tenantId.HasValue)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Failure(ApiMessages.Forbidden));
        }

        var userId = Guid.TryParse(currentUser.ExternalUserId, out var parsed) ? parsed : Guid.Empty;
        var result = await mediator.Send(new DismissPendingApprovalCommand(approvalId, tenantId.Value, userId), cancellationToken);

        return Ok(ApiResponse<DismissApprovalResultDto>.Ok(result, ApiMessages.ActionDismissed));
    }

    /// <summary>
    /// Retrieves active pending approval requests awaiting confirmation in this workspace.
    /// </summary>
    [HttpGet("pending")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<IReadOnlyList<PendingApprovalDto>>))]
    public async Task<IActionResult> GetPending(
        [FromQuery] Guid? conversationId,
        CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;
        if (!tenantId.HasValue)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Failure(ApiMessages.Forbidden));
        }

        var userId = Guid.TryParse(currentUser.ExternalUserId, out var parsed) ? parsed : Guid.Empty;
        var result = await mediator.Send(new GetPendingApprovalsQuery(tenantId.Value, userId, conversationId), cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<PendingApprovalDto>>.Ok(result));
    }
}
