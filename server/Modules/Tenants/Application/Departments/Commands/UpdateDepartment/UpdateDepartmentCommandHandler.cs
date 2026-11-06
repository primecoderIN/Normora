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
        if (!tenantContext.TenantId.HasValue) throw new InvalidOperationException(Normora.Shared.Constants.ApiMessages.TenantContextMissing);

        var department = await dbContext.Departments
            .FirstOrDefaultAsync(d => d.Id == request.Id && d.TenantId == tenantContext.TenantId.Value, cancellationToken);

        if (department == null)
        {
            throw new BolaException();
        }

        // Enforce unique name per tenant (excluding self)
        var exists = await dbContext.Departments
            .AnyAsync(d => d.TenantId == tenantContext.TenantId.Value 
                           && d.Id != request.Id 
                           && d.Name.ToLower() == request.Name.ToLower(), cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException(Normora.Shared.Constants.ApiMessages.DepartmentExists);
        }

        department.Name = request.Name;
        department.Description = request.Description;
        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}

