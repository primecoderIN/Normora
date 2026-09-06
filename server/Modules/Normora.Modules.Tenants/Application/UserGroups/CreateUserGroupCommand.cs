using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Domain;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.UserGroups;

public record CreateUserGroupCommand(string Name, List<Guid> DepartmentIds) : IRequest<Guid>;

public class CreateUserGroupCommandValidator : AbstractValidator<CreateUserGroupCommand>
{
    public CreateUserGroupCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DepartmentIds).NotNull();
    }
}

public class CreateUserGroupCommandHandler(TenantsDbContext dbContext, ITenantContext tenantContext)
    : IRequestHandler<CreateUserGroupCommand, Guid>
{
    public async Task<Guid> Handle(CreateUserGroupCommand request, CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue) throw new InvalidOperationException("Tenant Context missing.");

        var exists = await dbContext.UserGroups
            .AnyAsync(g => g.TenantId == tenantContext.TenantId.Value && g.Name.ToLower() == request.Name.ToLower(), cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException("A User Group with this name already exists.");
        }

        var group = new UserGroup
        {
            Id = Guid.NewGuid(),
            TenantId = tenantContext.TenantId.Value,
            Name = request.Name,
            UserGroupDepartments = request.DepartmentIds.Select(depId => new UserGroupDepartment
            {
                DepartmentId = depId
            }).ToList()
        };

        dbContext.UserGroups.Add(group);
        await dbContext.SaveChangesAsync(cancellationToken);

        return group.Id;
    }
}
