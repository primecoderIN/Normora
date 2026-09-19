using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Users;

/// <summary>
/// Handles <see cref="GetCurrentUserQuery"/> by loading the user from the database and syncing any profile changes from Keycloak.
/// </summary>
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

        var pendingInvitations = new List<PendingInvitationDto>();
        if (!string.IsNullOrEmpty(currentUser.Email))
        {
            var invites = await context.TenantInvitations
                .Include(i => i.Tenant)
                .Where(i => i.Email == currentUser.Email 
                         && i.Status == Normora.Modules.Tenants.Domain.InvitationStatus.Pending 
                         && i.ExpiresAt > DateTime.UtcNow)
                .ToListAsync(cancellationToken);

            pendingInvitations = invites.Select(i => new PendingInvitationDto(i.Token, i.Tenant.Name)).ToList();
        }

        if (user == null)
        {
            // Auto-provision the user and their Personal Workspace.
            user = new Normora.Modules.Tenants.Domain.User
            {
                Id = Guid.NewGuid(),
                KeycloakUserId = currentUser.KeycloakUserId,
                Email = currentUser.Email ?? string.Empty,
                DisplayName = currentUser.DisplayName ?? string.Empty,
                CreatedAt = DateTime.UtcNow
            };

            var personalTenant = new Normora.Modules.Tenants.Domain.Tenant
            {
                Id = Guid.NewGuid(),
                Name = "Personal Workspace",
                Slug = $"personal-{user.Id.ToString().Substring(0, 8)}",
                Status = Normora.Modules.Tenants.Domain.TenantStatus.Active,
                IsPersonal = true,
                CreatedAt = DateTime.UtcNow
            };

            var membership = new Normora.Modules.Tenants.Domain.TenantMembership
            {
                UserId = user.Id,
                TenantId = personalTenant.Id,
                Role = Normora.Modules.Tenants.Domain.MembershipRole.Admin,
                JoinedAt = DateTime.UtcNow
            };

            context.Users.Add(user);
            context.Tenants.Add(personalTenant);
            context.TenantMemberships.Add(membership);

            await context.SaveChangesAsync(cancellationToken);

            // Re-fetch or manually construct the membership for the DTO
            user.Memberships = new List<Normora.Modules.Tenants.Domain.TenantMembership> { membership };
            membership.Tenant = personalTenant;
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
            m.Role.ToString().ToLowerInvariant(),
            m.Tenant.IsPersonal
        )).ToList();

        return new CurrentUserDto(user.Id, user.Email ?? string.Empty, user.DisplayName ?? string.Empty, memberships, pendingInvitations);
    }
}
