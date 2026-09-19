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
    : IRequestHandler<CreateTenantCommand, TenantDto>
{
    public async Task<TenantDto> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        // Ensure the user is fully authenticated before allowing them to spin up a new organization
        if (!currentUser.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("You must be logged in to create a tenant.");
        }

        var keycloakId = currentUser.KeycloakUserId;

        // Start a database transaction so we can safely roll back if creating the user, tenant, or membership fails midway
        using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Check if this Keycloak user already has a local profile in our database, and create one if they don't
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

            // Verify that the requested workspace URL slug isn't already taken by another organization
            if (await dbContext.Tenants.AnyAsync(t => t.Slug == request.Slug, cancellationToken))
            {
                throw new InvalidOperationException($"Tenant slug '{request.Slug}' is already taken.");
            }

            // Provision the new workspace entity
            var tenant = new Tenant
            {
                Name = request.Name,
                Slug = request.Slug
            };
            dbContext.Tenants.Add(tenant);
            await dbContext.SaveChangesAsync(cancellationToken);

            // Immediately grant the creator full Admin privileges over their newly minted workspace
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

            return new TenantDto(tenant.Id, tenant.Name, tenant.Slug, (int)tenant.Status, tenant.CreatedAt);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
