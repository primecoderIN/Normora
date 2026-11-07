using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Domain;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Invitations.Commands;

/// <summary>
/// Handles <see cref="RejectInvitationCommand"/> by validating the token, verifying the recipient's email,
/// and marking the invitation as Rejected.
/// </summary>
public class RejectInvitationCommandHandler(TenantsDbContext context, ICurrentUser currentUser) : IRequestHandler<RejectInvitationCommand, bool>
{
    public async Task<bool> Handle(RejectInvitationCommand request, CancellationToken cancellationToken)
    {
        // Authentication Guard:
        // Rejecting an invitation requires a valid authenticated identity session to confirm the actor's email.
        if (!currentUser.IsAuthenticated)
        {
            throw new UnauthorizedAccessException(Normora.Shared.Constants.ApiMessages.Unauthorized);
        }

        // Token Lookup:
        // Find the invitation record matching the secure cryptographically-generated token.
        var invitation = await context.TenantInvitations
            .FirstOrDefaultAsync(i => i.Token == request.Token, cancellationToken);

        if (invitation == null)
        {
            throw new InvalidOperationException(Normora.Shared.Constants.ApiMessages.InvitationInvalidOrMissing);
        }

        // Idempotency and State Transition Guard:
        // Prevent re-processing an invitation that has already been accepted, revoked, or rejected.
        if (invitation.Status != InvitationStatus.Pending)
        {
            throw new InvalidOperationException(Normora.Shared.Constants.ApiMessages.InvitationAlreadyProcessed);
        }

        // Expiration Verification:
        // Invitations have a strict time-to-live (default 7 days). Expired invitations cannot be processed.
        if (invitation.ExpiresAt < DateTime.UtcNow)
        {
            throw new InvalidOperationException(Normora.Shared.Constants.ApiMessages.InvitationExpired);
        }

        // Anti-Account-Hijacking Identity Guard:
        // Crucial security control: verify that the currently authenticated user's email matches
        // the email address that the invitation was explicitly sent to. This prevents one user
        // from intercepting or rejecting another employee's pending invitation link.
        if (!string.Equals(invitation.Email, currentUser.Email, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(Normora.Shared.Constants.ApiMessages.InvitationEmailMismatch);
        }

        // Update state to Rejected
        invitation.Status = InvitationStatus.Rejected;

        await context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
