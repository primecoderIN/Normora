using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Domain;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Invitations;

public class AcceptInvitationCommandHandler(TenantsDbContext context, ICurrentUser currentUser) : IRequestHandler<AcceptInvitationCommand, bool>
{
    public async Task<bool> Handle(AcceptInvitationCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("You must be authenticated to accept an invitation.");
        }

        var invitation = await context.TenantInvitations
            .FirstOrDefaultAsync(i => i.Token == request.Token, cancellationToken);

        if (invitation == null || invitation.Status != "Pending" || invitation.ExpiresAt < DateTime.UtcNow)
        {
            return false;
        }

        // Validate the email matches the authenticated user's email
        // In some systems, this is relaxed, but for security, we enforce email matching.
        if (!string.Equals(invitation.Email, currentUser.Email, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("This invitation was sent to a different email address.");
        }

        // Get or create the user in the Tenants DB
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

        // Check if they are already a member
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

        invitation.Status = "Accepted";

        await context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
