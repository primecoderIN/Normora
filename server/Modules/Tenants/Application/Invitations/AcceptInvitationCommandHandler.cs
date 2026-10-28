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
            throw new InvalidOperationException(Normora.Shared.Constants.ApiMessages.InvitationAlreadyAccepted);
        }

        if (invitation.ExpiresAt < DateTime.UtcNow)
        {
            throw new InvalidOperationException(Normora.Shared.Constants.ApiMessages.InvitationExpired);
        }

        // Prevent invitation hijacking by ensuring the logged-in user's email matches the email the invite was sent to
        if (!string.Equals(invitation.Email, currentUser.Email, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(Normora.Shared.Constants.ApiMessages.InvitationEmailMismatch);
        }

        // Get or create the local user profile for the invitee if this is their first time logging in
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.ExternalUserId == currentUser.ExternalUserId, cancellationToken);

        if (user == null)
        {
            user = new User
            {
                ExternalUserId = currentUser.ExternalUserId,
                Email = currentUser.Email,
                DisplayName = currentUser.DisplayName ?? string.Empty
            };
            context.Users.Add(user);
        }

        // Make the acceptance idempotent by skipping membership creation if active, or reactivating if soft-deleted
        var existingMembership = await context.TenantMemberships
            .IgnoreQueryFilters()
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
        else if (existingMembership.RemovedAt != null)
        {
            // Reactivate soft-deleted membership
            existingMembership.RemovedAt = null;
            existingMembership.RemovedByUserId = null;
            existingMembership.Role = TenantRole.Employee;
        }


        invitation.Status = InvitationStatus.Accepted;

        await context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
