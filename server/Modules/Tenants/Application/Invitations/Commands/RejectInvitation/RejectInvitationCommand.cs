using MediatR;

namespace Normora.Modules.Tenants.Application.Invitations.Commands;

/// <summary>
/// Command to explicitly decline or reject a pending organization membership invitation.
/// Handled by <see cref="RejectInvitationCommandHandler"/>.
/// </summary>
/// <remarks>
/// Security Workflow:
/// - Verifies that the invitation token exists and remains in "Pending" status.
/// - Transitions status to "Rejected" so the token cannot be accepted in subsequent attempts.
/// - Prevents spam or unauthorized workspace association.
/// </remarks>
/// <param name="Token">The unique invitation token being rejected.</param>
public record RejectInvitationCommand(Guid Token) : IRequest<bool>;
