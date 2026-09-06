using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Domain;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.UserGroups;

public record UpdateUserGroupCommand(Guid Id, string Name, List<Guid> DepartmentIds) : IRequest<bool>;

public class UpdateUserGroupCommandValidator : AbstractValidator<UpdateUserGroupCommand>
{
    public UpdateUserGroupCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DepartmentIds).NotNull();
    }
}

public class UpdateUserGroupCommandHandler(TenantsDbContext dbContext, ITenantContext tenantContext)
    : IRequestHandler<UpdateUserGroupCommand, bool>
{
    public async Task<bool> Handle(UpdateUserGroupCommand request, CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue) throw new InvalidOperationException("Tenant Context missing.");

        var group = await dbContext.UserGroups
            .Include(g => g.UserGroupDepartments)
            .FirstOrDefaultAsync(g => g.Id == request.Id && g.TenantId == tenantContext.TenantId.Value, cancellationToken);

        if (group == null)
        {
            return false;
        }

        var exists = await dbContext.UserGroups
            .AnyAsync(g => g.TenantId == tenantContext.TenantId.Value 
                           && g.Id != request.Id 
                           && g.Name.ToLower() == request.Name.ToLower(), cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException("A User Group with this name already exists.");
        }

        group.Name = request.Name;

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
