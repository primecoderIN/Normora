using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Exceptions;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Departments.Commands;

/// <summary>
/// Handles the <see cref="DeleteDepartmentCommand"/> by finding and removing the specified department.
/// </summary>
public class DeleteDepartmentCommandHandler(TenantsDbContext dbContext, ITenantContext tenantContext)
    : IRequestHandler<DeleteDepartmentCommand, bool>
{
    public async Task<bool> Handle(DeleteDepartmentCommand request, CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue) throw new InvalidOperationException(Normora.Shared.Constants.ApiMessages.TenantContextMissing);

        var department = await dbContext.Departments
            .FirstOrDefaultAsync(d => d.Id == request.Id && d.TenantId == tenantContext.TenantId.Value, cancellationToken);

        if (department == null)
        {
            throw new BolaException();
        }

        dbContext.Departments.Remove(department);
        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}

