using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Users.Queries;

/// <summary>
/// Handles <see cref="GetCurrentUserQuery"/> by loading the user from the database and syncing any profile changes from Keycloak.
/// </summary>
public class GetCurrentUserQueryHandler(TenantsDbContext context, ICurrentUser currentUser) : IRequestHandler<GetCurrentUserQuery, CurrentUserDto>
{
    public async Task<CurrentUserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        // Authentication Guard:
        // Ensures that claims principal has been populated by the Duende BFF / JWT bearer authentication scheme.
        if (!currentUser.IsAuthenticated)
        {
            throw new UnauthorizedAccessException(Normora.Shared.Constants.ApiMessages.Unauthorized);
        }

        // User & Workspace Membership Resolution:
        // Finds the internal User entity matching the external identity provider subject (ExternalUserId).
        // Eagerly includes TenantMemberships and their corresponding Tenant details to evaluate workspace access.
        var user = await context.Users
            .Include(u => u.Memberships)
            .ThenInclude(m => m.Tenant)
            .FirstOrDefaultAsync(u => u.ExternalUserId == currentUser.ExternalUserId, cancellationToken);

        // Pending Invitations Lookup:
        // Checks if the user's verified email has unaccepted invitations for other organizational workspaces.
        // These are returned so the client onboarding/dashboard can render one-click "Accept / Reject Invitation" banners.
        var pendingInvitations = new List<PendingInvitationDto>();
        if (!string.IsNullOrEmpty(currentUser.Email))
        {
            var invites = await context.TenantInvitations
                .Include(i => i.Tenant)
                .Where(i => EF.Functions.ILike(i.Email, currentUser.Email) 
                         && i.Status == Normora.Modules.Tenants.Domain.InvitationStatus.Pending 
                         && i.ExpiresAt > DateTime.UtcNow)
                .ToListAsync(cancellationToken);

            pendingInvitations = invites.Select(i => new PendingInvitationDto(i.Token, i.Tenant.Name)).ToList();
        }

        // Just-In-Time (JIT) Provisioning Workflow:
        // When a user authenticates via OIDC for the very first time, they will not yet exist in the local database.
        // We automatically provision:
        // 1. An internal User profile entity with their OIDC claims (ExternalUserId, Email, DisplayName).
        // 2. A default "Personal Workspace" tenant (IsPersonal = true) so they can immediately begin using the system.
        // 3. An initial TenantMembership assigning them the TenantRole.Admin role within their personal workspace.
        if (user == null)
        {
            var newUser = new Normora.Modules.Tenants.Domain.User
            {
                Id = Guid.NewGuid(),
                ExternalUserId = currentUser.ExternalUserId,
                Email = currentUser.Email ?? string.Empty,
                DisplayName = currentUser.DisplayName ?? string.Empty,
                CreatedAt = DateTime.UtcNow
            };

            var personalTenant = new Normora.Modules.Tenants.Domain.Tenant
            {
                Id = Guid.NewGuid(),
                Name = "Personal Workspace",
                Slug = $"personal-{newUser.Id.ToString().Substring(0, 8)}",
                Status = Normora.Modules.Tenants.Domain.TenantStatus.Active,
                IsPersonal = true,
                CreatedAt = DateTime.UtcNow
            };

            var membership = new Normora.Modules.Tenants.Domain.TenantMembership
            {
                UserId = newUser.Id,
                TenantId = personalTenant.Id,
                Role = Normora.Modules.Tenants.Domain.TenantRole.Admin
            };

            try
            {
                context.Users.Add(newUser);
                context.Tenants.Add(personalTenant);
                context.TenantMemberships.Add(membership);

                await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                // Concurrency & Race Condition Handling:
                // If two parallel requests fire simultaneously upon initial login (e.g. BFF user info + SignalR handshake),
                // one request will succeed while the second throws a unique key violation on ExternalUserId.
                // We safely catch the DbUpdateException, detach the failed in-memory entities, and re-fetch the persisted user.
                context.Entry(newUser).State = EntityState.Detached;
                context.Entry(personalTenant).State = EntityState.Detached;
                context.Entry(membership).State = EntityState.Detached;
            }

            user = await context.Users
                .Include(u => u.Memberships)
                .ThenInclude(m => m.Tenant)
                .FirstAsync(u => u.ExternalUserId == currentUser.ExternalUserId, cancellationToken);
        }

        // Identity Claims Synchronization (OIDC Claims Sync):
        // If the user modified their display name or email in the identity provider (e.g. Keycloak / Google),
        // we detect the divergence and synchronize the local database record to maintain directory consistency.
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

        // Workspace Membership Projection:
        // Formats the user's active tenant memberships with roles and personal workspace flags for client-side state.
        var memberships = user.Memberships.Select(m => new UserTenantMembershipDto(
            m.TenantId,
            m.Tenant.Name,
            m.Tenant.Slug,
            m.Role.ToString().ToLowerInvariant(),
            m.Tenant.IsPersonal
        )).ToList();

        return new CurrentUserDto(user.Id, user.Email ?? string.Empty, user.DisplayName ?? string.Empty, user.CreatedAt, memberships, pendingInvitations);
    }
}
