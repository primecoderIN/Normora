using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Domain;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Invitations.Commands;

/// <summary>
/// Handles <see cref="AcceptInvitationCommand"/> by validating the token, verifying the recipient's email,
/// creating the user record if needed, and adding a TenantMembership.
/// </summary>
public class AcceptInvitationCommandHandler(
    TenantsDbContext context,
    ICurrentUser currentUser,
    ICacheService? cache = null) : IRequestHandler<AcceptInvitationCommand, bool>
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

        // Anti-hijacking guard: the authenticated user's email must strictly match the invited email address.
        if (!string.Equals(invitation.Email, currentUser.Email, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(Normora.Shared.Constants.ApiMessages.InvitationEmailMismatch);
        }

        // Just-in-Time user provisioning: synchronize the shadow profile on first workspace onboarding.
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

        // Idempotent membership handling: bypass query filters to reactivate soft-deleted members without violating unique constraints.
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
            // Reactivate previous membership while resetting audit removal markers.
            existingMembership.RemovedAt = null;
            existingMembership.RemovedByUserId = null;
            existingMembership.Role = TenantRole.Employee;
        }


        invitation.Status = InvitationStatus.Accepted;

        await context.SaveChangesAsync(cancellationToken);

        // Immediate Onboarding Clearance:
        // Evict any stale negative or transient resolution cache entry for this user so their new
        // membership and employee role become active on their very next API call.
        if (cache != null && !string.IsNullOrWhiteSpace(currentUser.ExternalUserId))
        {
            await cache.RemoveAsync($"tenant_access:{invitation.TenantId:N}:{currentUser.ExternalUserId}", cancellationToken);
            await cache.RemoveAsync($"dashboard:{invitation.TenantId}", cancellationToken);
        }

        return true;
    }
}
