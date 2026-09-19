using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Domain;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Invitations;

/// <summary>
/// Handles <see cref="AcceptInvitationCommand"/> by validating the token, verifying the recipient's email,
/// creating the user record if needed, and adding a TenantMembership.
/// </summary>
public class AcceptInvitationCommandHandler(TenantsDbContext context, ICurrentUser currentUser) : IRequestHandler<AcceptInvitationCommand, bool>
{
    public async Task<bool> Handle(AcceptInvitationCommand request, CancellationToken cancellationToken)
    {
        // Verify the user is authenticated since invitations can only be accepted by logged-in users
        if (!currentUser.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("You must be authenticated to accept an invitation.");
        }

        var invitation = await context.TenantInvitations
            .FirstOrDefaultAsync(i => i.Token == request.Token, cancellationToken);

        if (invitation == null)
        {
            throw new InvalidOperationException("This invitation is invalid or does not exist.");
        }

        if (invitation.Status != InvitationStatus.Pending)
        {
            throw new InvalidOperationException("This invitation has already been accepted or is no longer valid.");
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

        // Get or create the local user profile for the invitee if this is their first time logging in
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.KeycloakUserId == currentUser.KeycloakUserId, cancellationToken);

        if (user == null)
        {
            user = new User
            {
                KeycloakUserId = currentUser.KeycloakUserId,
                Email = currentUser.Email,
                DisplayName = currentUser.DisplayName ?? "Invited User"
            };
            context.Users.Add(user);
        }

        // Make the acceptance idempotent by skipping membership creation if they are already a member of this workspace
        var existingMembership = await context.TenantMemberships
            .FirstOrDefaultAsync(m => m.TenantId == invitation.TenantId && m.UserId == user.Id, cancellationToken);

        if (existingMembership == null)
        {
            var membership = new TenantMembership
            {
                TenantId = invitation.TenantId,
                User = user,
                Role = TenantRole.Employee
            };
            context.TenantMemberships.Add(membership);
        }

        invitation.Status = InvitationStatus.Accepted;

        await context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
