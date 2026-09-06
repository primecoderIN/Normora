using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Departments;

public record DeleteDepartmentCommand(Guid Id) : IRequest<bool>;

public class DeleteDepartmentCommandHandler(TenantsDbContext dbContext, ITenantContext tenantContext)
    : IRequestHandler<DeleteDepartmentCommand, bool>
{
    public async Task<bool> Handle(DeleteDepartmentCommand request, CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue) throw new InvalidOperationException("Tenant Context missing.");

        var department = await dbContext.Departments
            .FirstOrDefaultAsync(d => d.Id == request.Id && d.TenantId == tenantContext.TenantId.Value, cancellationToken);

        if (department == null)
        {
            return false;
        }

        dbContext.Departments.Remove(department);
        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
