using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Domain;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Invitations;

/// <summary>
/// Handles <see cref="RejectInvitationCommand"/> by validating the token, verifying the recipient's email,
/// and marking the invitation as Rejected.
/// </summary>
public class RejectInvitationCommandHandler(TenantsDbContext context, ICurrentUser currentUser) : IRequestHandler<RejectInvitationCommand, bool>
{
    public async Task<bool> Handle(RejectInvitationCommand request, CancellationToken cancellationToken)
    {
        // Verify the user is authenticated
        if (!currentUser.IsAuthenticated)
        {
            throw new UnauthorizedAccessException(Normora.Shared.Constants.ApiMessages.Unauthorized);
        }

        var invitation = await context.TenantInvitations
            .FirstOrDefaultAsync(i => i.Token == request.Token, cancellationToken);

        if (invitation == null)
        {
            throw new InvalidOperationException(Normora.Shared.Constants.ApiMessages.InvitationInvalidOrMissing);
        }

        if (invitation.Status != InvitationStatus.Pending)
        {
            throw new InvalidOperationException(Normora.Shared.Constants.ApiMessages.InvitationAlreadyProcessed);
        }

        if (invitation.ExpiresAt < DateTime.UtcNow)
        {
            throw new InvalidOperationException(Normora.Shared.Constants.ApiMessages.InvitationExpired);
        }

        if (!string.Equals(invitation.Email, currentUser.Email, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(Normora.Shared.Constants.ApiMessages.InvitationEmailMismatch);
        }

        invitation.Status = InvitationStatus.Rejected;

        await context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
