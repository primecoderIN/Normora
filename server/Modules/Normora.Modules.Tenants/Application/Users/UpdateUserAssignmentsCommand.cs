using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Domain;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Users;

public record UpdateUserAssignmentsCommand(Guid UserId, List<Guid> DepartmentIds, List<Guid> UserGroupIds) : IRequest<bool>;

public class UpdateUserAssignmentsCommandValidator : AbstractValidator<UpdateUserAssignmentsCommand>
{
    public UpdateUserAssignmentsCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.DepartmentIds).NotNull();
        RuleFor(x => x.UserGroupIds).NotNull();
    }
}

public class UpdateUserAssignmentsCommandHandler(TenantsDbContext dbContext, ITenantContext tenantContext)
    : IRequestHandler<UpdateUserAssignmentsCommand, bool>
{
    public async Task<bool> Handle(UpdateUserAssignmentsCommand request, CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue) throw new InvalidOperationException("Tenant Context missing.");

        var membership = await dbContext.TenantMemberships
            .Include(m => m.MembershipDepartments)
            .Include(m => m.UserGroupMemberships)
            .FirstOrDefaultAsync(m => m.UserId == request.UserId && m.TenantId == tenantContext.TenantId.Value, cancellationToken);

        if (membership == null)
        {
            return false;
        }

        // Sync Departments
        membership.MembershipDepartments.Clear();
        foreach (var depId in request.DepartmentIds)
        {
            membership.MembershipDepartments.Add(new MembershipDepartment { DepartmentId = depId });
        }

        // Sync User Groups
        membership.UserGroupMemberships.Clear();
        foreach (var groupId in request.UserGroupIds)
        {
            membership.UserGroupMemberships.Add(new UserGroupMembership { UserGroupId = groupId });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
