using MediatR;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Invitations.Commands;

/// <summary>
/// Command to accept a pending tenant invitation using a cryptographically random token.
/// Handled by <see cref="AcceptInvitationCommandHandler"/>.
/// </summary>
/// <remarks>
/// Workflow & Security Invariants:
/// - Resolves the invitation record via global unique token (independent of ambient tenant context).
/// - Validates that status is "Pending" and UTC expiration time has not elapsed (48h window).
/// - Reconciles caller's external authenticated OIDC email with the invited recipient email.
/// - Provisions or reactivates a <see cref="Domain.TenantMembership"/> record with the Employee role.
/// - Marks invitation status as "Accepted" and records an audit log entry.
/// </remarks>
/// <param name="Token">The unique invitation token sent to the employee's email.</param>
public record AcceptInvitationCommand(Guid Token) : IRequest<bool>, IAuditableRequest
{
    public string Action => "Invitation.Accepted";
    public string TargetType => "Invitation";
    public string? GetTargetId(object? response) => Token.ToString();
    public string? GetMetadata(object? response) => null;
}

