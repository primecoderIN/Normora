using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Domain;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Departments.Commands;

/// <summary>
/// Handles the <see cref="CreateDepartmentCommand"/> by ensuring name uniqueness and saving the new department.
/// </summary>
public class CreateDepartmentCommandHandler(TenantsDbContext dbContext, ITenantContext tenantContext)
    : IRequestHandler<CreateDepartmentCommand, Guid>
{
    public async Task<Guid> Handle(CreateDepartmentCommand request, CancellationToken cancellationToken)
    {
        // Multi-tenant ambient context validation:
        // Every administrative department mutation requires a resolved TenantId from TenantResolutionMiddleware.
        // If missing or unauthenticated, throw InvalidOperationException to halt execution immediately.
        if (!tenantContext.TenantId.HasValue) throw new InvalidOperationException(Normora.Shared.Constants.ApiMessages.TenantContextMissing);

        // Case-insensitive department name uniqueness constraint:
        // Departments represent strict organizational boundaries for ACL and conversational RAG retrieval.
        // We enforce uniqueness per tenant to prevent duplicate routing filters and employee assignment confusion.
        var exists = await dbContext.Departments
            .AnyAsync(d => d.TenantId == tenantContext.TenantId.Value && d.Name.ToLower() == request.Name.ToLower(), cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException(Normora.Shared.Constants.ApiMessages.DepartmentExists);
        }

        // Entity instantiation and primary key generation:
        // Generate a new GUID client-side to ensure immediate identity reference before persistence.
        // The TenantId is strictly bound to the authenticated ambient context to prevent cross-tenant leakage.
        var department = new Department
        {
            Id = Guid.NewGuid(),
            TenantId = tenantContext.TenantId.Value,
            Name = request.Name,
            Description = request.Description
        };

        dbContext.Departments.Add(department);
        await dbContext.SaveChangesAsync(cancellationToken);

        return department.Id;
    }
}

