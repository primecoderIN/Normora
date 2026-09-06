using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Domain;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Users;

/// <summary>
/// Command to update a user's department and user group assignments within the current tenant.
/// </summary>
/// <param name="UserId">The ID of the user whose assignments are being updated.</param>
/// <param name="DepartmentIds">The new list of directly assigned department IDs.</param>
/// <param name="UserGroupIds">The new list of user group IDs the user should belong to.</param>
public record UpdateUserAssignmentsCommand(Guid UserId, List<Guid> DepartmentIds, List<Guid> UserGroupIds) : IRequest<bool>;

/// <summary>
/// Validates the <see cref="UpdateUserAssignmentsCommand"/>.
/// </summary>
public class UpdateUserAssignmentsCommandValidator : AbstractValidator<UpdateUserAssignmentsCommand>
{
    public UpdateUserAssignmentsCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.DepartmentIds).NotNull();
        RuleFor(x => x.UserGroupIds).NotNull();
    }
}

/// <summary>
/// Handles <see cref="UpdateUserAssignmentsCommand"/> by synchronizing the user's department and group memberships.
/// </summary>
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
