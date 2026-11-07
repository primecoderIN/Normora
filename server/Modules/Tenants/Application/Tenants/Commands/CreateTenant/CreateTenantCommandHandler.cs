using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Domain;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Tenants.Commands;

/// <summary>
/// Handles the creation of a new Tenant.
/// This command ensures that the user creating the tenant is recorded as an Admin for that new tenant.
/// </summary>
/// <param name="dbContext">The Tenants module DbContext.</param>
/// <param name="currentUser">The service providing the current authenticated OIDC user identity.</param>
public class CreateTenantCommandHandler(TenantsDbContext dbContext, ICurrentUser currentUser) 
    : IRequestHandler<CreateTenantCommand, TenantDto>
{
    public async Task<TenantDto> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        // Enforce authentication before permitting tenant provisioning.
        if (!currentUser.IsAuthenticated)
        {
            throw new UnauthorizedAccessException(Normora.Shared.Constants.ApiMessages.Unauthorized);
        }

        var externalUserId = currentUser.ExternalUserId;

        // Atomic transaction guarantees tenant, creator membership, and subscription plan are provisioned together.
        using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Just-in-Time user profile synchronization on workspace creation.
            var user = await dbContext.Users
                .FirstOrDefaultAsync(u => u.ExternalUserId == externalUserId, cancellationToken);

            if (user == null)
            {
                user = new User
                {
                    ExternalUserId = externalUserId,
                    Email = currentUser.Email,
                    DisplayName = currentUser.DisplayName
                };
                dbContext.Users.Add(user);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            // Ensure globally unique workspace slug for path-based routing.
            if (await dbContext.Tenants.AnyAsync(t => t.Slug == request.Slug, cancellationToken))
            {
                throw new InvalidOperationException(Normora.Shared.Constants.ApiMessages.TenantSlugTaken);
            }

            // Provision workspace with active status.
            var tenant = new Tenant
            {
                Name = request.Name,
                Slug = request.Slug
            };
            dbContext.Tenants.Add(tenant);
            await dbContext.SaveChangesAsync(cancellationToken);

            // Grant creator Administrator role over the new workspace.
            var membership = new TenantMembership
            {
                TenantId = tenant.Id,
                UserId = user.Id,
                Role = TenantRole.Admin
            };
            dbContext.TenantMemberships.Add(membership);

            // Initialize default subscription plan quotas (Free tier).
            var plan = TenantPlan.CreateDefault(tenant.Id);
            dbContext.TenantPlans.Add(plan);

            await dbContext.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return new TenantDto(tenant.Id, tenant.Name, tenant.Slug, (int)tenant.Status, tenant.CreatedAt);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
