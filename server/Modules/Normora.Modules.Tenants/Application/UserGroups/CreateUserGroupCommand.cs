using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Domain;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.UserGroups;

/// <summary>
/// Command to create a new user group within the current tenant and assign it to departments.
/// </summary>
/// <param name="Name">The name of the user group.</param>
/// <param name="DepartmentIds">A list of department IDs this group should have access to.</param>
public record CreateUserGroupCommand(string Name, List<Guid> DepartmentIds) : IRequest<Guid>;

/// <summary>
/// Validates the <see cref="CreateUserGroupCommand"/>.
/// </summary>
public class CreateUserGroupCommandValidator : AbstractValidator<CreateUserGroupCommand>
{
    public CreateUserGroupCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DepartmentIds).NotNull();
    }
}

/// <summary>
/// Handles the <see cref="CreateUserGroupCommand"/> by ensuring name uniqueness and saving the group with its department associations.
/// </summary>
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
