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
            throw new InvalidOperationException("This invitation is invalid or does not exist.");
        }

        if (invitation.Status != InvitationStatus.Pending)
        {
            throw new InvalidOperationException("This invitation has already been processed.");
        }

        if (invitation.ExpiresAt < DateTime.UtcNow)
        {
            throw new InvalidOperationException("This invitation link has expired.");
        }

        // Prevent invitation hijacking by ensuring the logged-in user's email matches the email the invite was sent to
        if (!string.Equals(invitation.Email, currentUser.Email, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("This invitation was sent to a different email address.");
        }

        invitation.Status = InvitationStatus.Rejected;

        await context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
