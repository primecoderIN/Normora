using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Users;

public class GetCurrentUserQueryHandler(TenantsDbContext context, ICurrentUser currentUser) : IRequestHandler<GetCurrentUserQuery, CurrentUserDto>
{
    public async Task<CurrentUserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        var user = await context.Users
            .Include(u => u.Memberships)
            .ThenInclude(m => m.Tenant)
            .FirstOrDefaultAsync(u => u.KeycloakUserId == currentUser.KeycloakUserId, cancellationToken);

        if (user == null)
        {
            // If the user doesn't exist in our DB yet, they just signed up via Keycloak.
            // We should create a baseline record for them here, or return an empty profile.
            // Returning an empty profile so the frontend knows they need to onboard.
            return new CurrentUserDto(Guid.Empty, string.Empty, string.Empty, new List<UserTenantMembershipDto>());
        }

        // Sync any changes from Keycloak (like if they updated their name or email)
        bool isUpdated = false;
        
        if (user.DisplayName != currentUser.DisplayName)
        {
            user.DisplayName = currentUser.DisplayName;
            isUpdated = true;
        }

        if (user.Email != currentUser.Email)
        {
            user.Email = currentUser.Email;
            isUpdated = true;
        }

        if (isUpdated)
        {
            user.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
        }

        var memberships = user.Memberships.Select(m => new UserTenantMembershipDto(
            m.TenantId,
            m.Tenant.Name,
            m.Tenant.Slug,
            m.Role.ToString().ToLowerInvariant()
        )).ToList();

        return new CurrentUserDto(user.Id, user.Email ?? string.Empty, user.DisplayName ?? string.Empty, memberships);
    }
}
