using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Domain;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.CreateTenant;

/// <summary>
/// Handles the creation of a new Tenant.
/// This command ensures that the user creating the tenant is recorded as an Admin for that new tenant.
/// </summary>
/// <param name="dbContext">The Tenants module DbContext.</param>
/// <param name="currentUser">The service providing the current JWT Keycloak user identity.</param>
public class CreateTenantCommandHandler(TenantsDbContext dbContext, ICurrentUser currentUser) 
    : IRequestHandler<CreateTenantCommand, Tenant>
{
    public async Task<Tenant> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        // 1. Ensure the caller is an authenticated user.
        if (!currentUser.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("You must be logged in to create a tenant.");
        }

        var keycloakId = currentUser.KeycloakUserId;

        // Start a database transaction because we are creating multiple related entities (User, Tenant, Membership).
        // If any step fails, everything is rolled back to maintain consistency.
        using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // 2. Resolve or Create Normora User (TENANT-08)
            // The user exists in Keycloak, but might not exist in our Tenants database yet.
            var user = await dbContext.Users
                .FirstOrDefaultAsync(u => u.KeycloakUserId == keycloakId, cancellationToken);

            if (user == null)
            {
                user = new User
                {
                    KeycloakUserId = keycloakId,
                    Email = currentUser.Email,
                    DisplayName = currentUser.DisplayName
                };
                dbContext.Users.Add(user);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            // 3. Verify that the requested tenant slug is globally unique.
            if (await dbContext.Tenants.AnyAsync(t => t.Slug == request.Slug, cancellationToken))
            {
                throw new Exception($"Tenant slug '{request.Slug}' is already taken.");
            }

            // 4. Create the new Tenant entity.
            var tenant = new Tenant
            {
                Name = request.Name,
                Slug = request.Slug
            };
            dbContext.Tenants.Add(tenant);
            await dbContext.SaveChangesAsync(cancellationToken);

            // 5. Create TenantMembership (TENANT-10)
            // Immediately grant the creator the 'Admin' role within their new tenant.
            var membership = new TenantMembership
            {
                TenantId = tenant.Id,
                UserId = user.Id,
                Role = TenantRole.Admin
            };
            dbContext.TenantMemberships.Add(membership);

            await dbContext.SaveChangesAsync(cancellationToken);

            // Commit the transaction to save all changes.
            await transaction.CommitAsync(cancellationToken);

            return tenant;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
