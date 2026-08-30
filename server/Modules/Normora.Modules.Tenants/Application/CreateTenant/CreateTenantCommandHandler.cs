using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Domain;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.CreateTenant;

public class CreateTenantCommandHandler(TenantsDbContext dbContext, ICurrentUser currentUser) 
    : IRequestHandler<CreateTenantCommand, Tenant>
{
    public async Task<Tenant> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("You must be logged in to create a tenant.");
        }

        var keycloakId = currentUser.KeycloakUserId;

        // Start transaction
        using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // 1. Resolve or Create Normora User (TENANT-08)
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

            // 2. Check if slug is unique
            if (await dbContext.Tenants.AnyAsync(t => t.Slug == request.Slug, cancellationToken))
            {
                throw new Exception($"Tenant slug '{request.Slug}' is already taken.");
            }

            // 3. Create Tenant
            var tenant = new Tenant
            {
                Name = request.Name,
                Slug = request.Slug
            };
            dbContext.Tenants.Add(tenant);
            await dbContext.SaveChangesAsync(cancellationToken);

            // 4. Create TenantMembership (Admin) (TENANT-10)
            var membership = new TenantMembership
            {
                TenantId = tenant.Id,
                UserId = user.Id,
                Role = TenantRole.Admin
            };
            dbContext.TenantMemberships.Add(membership);

            await dbContext.SaveChangesAsync(cancellationToken);

            // Commit transaction
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
