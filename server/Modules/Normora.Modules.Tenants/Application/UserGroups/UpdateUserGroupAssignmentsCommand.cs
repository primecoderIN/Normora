using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Domain;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.UserGroups;

/// <summary>
/// Command to update the member users and department assignments of an existing user group.
/// </summary>
/// <param name="Id">The unique identifier of the user group.</param>
/// <param name="MemberUserIds">The list of user IDs to assign as members of this group.</param>
/// <param name="DepartmentIds">The list of department IDs to grant access to this group.</param>
public record UpdateUserGroupAssignmentsCommand(Guid Id, List<Guid> MemberUserIds, List<Guid> DepartmentIds) : IRequest<bool>;

/// <summary>
/// Validates the <see cref="UpdateUserGroupAssignmentsCommand"/>.
/// </summary>
public class UpdateUserGroupAssignmentsCommandValidator : AbstractValidator<UpdateUserGroupAssignmentsCommand>
{
    public UpdateUserGroupAssignmentsCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.MemberUserIds).NotNull();
        RuleFor(x => x.DepartmentIds).NotNull();
    }
}

/// <summary>
/// Handles the <see cref="UpdateUserGroupAssignmentsCommand"/> by synchronizing member and department associations.
/// </summary>
public class UpdateUserGroupAssignmentsCommandHandler(TenantsDbContext dbContext, ITenantContext tenantContext)
    : IRequestHandler<UpdateUserGroupAssignmentsCommand, bool>
{
    public async Task<bool> Handle(UpdateUserGroupAssignmentsCommand request, CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue) throw new InvalidOperationException("Tenant Context missing.");

        var group = await dbContext.UserGroups
            .Include(g => g.UserGroupMemberships)
            .Include(g => g.UserGroupDepartments)
            .FirstOrDefaultAsync(g => g.Id == request.Id && g.TenantId == tenantContext.TenantId.Value, cancellationToken);

        if (group == null)
        {
            return false;
        }

        // Resolve user IDs to tenant membership IDs
        var membershipIds = await dbContext.TenantMemberships
            .Where(tm => tm.TenantId == tenantContext.TenantId.Value && request.MemberUserIds.Contains(tm.UserId))
            .Select(tm => tm.Id)
            .ToListAsync(cancellationToken);

        // Sync members
        group.UserGroupMemberships.Clear();
        foreach (var memId in membershipIds)
        {
            group.UserGroupMemberships.Add(new UserGroupMembership { TenantMembershipId = memId });
        }

        // Sync departments
        group.UserGroupDepartments.Clear();
        foreach (var depId in request.DepartmentIds)
        {
            group.UserGroupDepartments.Add(new UserGroupDepartment { DepartmentId = depId });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
