using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Exceptions;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Departments.Commands;

/// <summary>
/// Handles the <see cref="UpdateDepartmentCommand"/> by updating the department's properties while ensuring uniqueness.
/// </summary>
public class UpdateDepartmentCommandHandler(TenantsDbContext dbContext, ITenantContext tenantContext)
    : IRequestHandler<UpdateDepartmentCommand, bool>
{
    public async Task<bool> Handle(UpdateDepartmentCommand request, CancellationToken cancellationToken)
    {
        // Multi-tenant ambient context validation:
        // Confirms that this mutation is executing within an authenticated, tenant-scoped request pipeline.
        if (!tenantContext.TenantId.HasValue) throw new InvalidOperationException(Normora.Shared.Constants.ApiMessages.TenantContextMissing);

        // Broken Object Level Authorization (BOLA) security guard:
        // We query the department by both Primary Key (Id) AND active TenantId.
        // If the department does not exist, or belongs to another tenant workspace, we throw a BolaException.
        // Global exception handling converts this to a generic 404 to block cross-tenant ID enumeration.
        var department = await dbContext.Departments
            .FirstOrDefaultAsync(d => d.Id == request.Id && d.TenantId == tenantContext.TenantId.Value, cancellationToken);

        if (department == null)
        {
            throw new BolaException();
        }

        // Case-insensitive name collision check excluding current entity:
        // Prevent renaming a department to an existing department name within the same tenant.
        var exists = await dbContext.Departments
            .AnyAsync(d => d.TenantId == tenantContext.TenantId.Value 
                           && d.Id != request.Id 
                           && d.Name.ToLower() == request.Name.ToLower(), cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException(Normora.Shared.Constants.ApiMessages.DepartmentExists);
        }

        // Synchronize updated metadata fields
        department.Name = request.Name;
        department.Description = request.Description;
        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}

