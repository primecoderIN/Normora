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
        // Multi-tenant ambient context validation:
        // Ensure request context is bound to an active tenant workspace.
        if (!tenantContext.TenantId.HasValue) throw new InvalidOperationException(Normora.Shared.Constants.ApiMessages.TenantContextMissing);

        // Broken Object Level Authorization (BOLA) guard:
        // Ensures administrators can only delete departments strictly belonging to their current active tenant.
        // If the ID is invalid or belongs to another workspace, BolaException produces a clean 404 response.
        var department = await dbContext.Departments
            .FirstOrDefaultAsync(d => d.Id == request.Id && d.TenantId == tenantContext.TenantId.Value, cancellationToken);

        if (department == null)
        {
            throw new BolaException();
        }

        // Relational cascade cleanup:
        // Removing this department cascades through EF Core and database foreign key configurations:
        // 1. Direct employee department memberships (MembershipDepartments) are purged.
        // 2. User group department associations (UserGroupDepartments) are unlinked.
        // 3. Document department access restrictions in the Documents module are decoupled.
        dbContext.Departments.Remove(department);
        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}

