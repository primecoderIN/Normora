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
public record CreateUserGroupCommand(string Name, string? Description, List<Guid> DepartmentIds) : IRequest<Guid>, IAuditableRequest
{
    public string Action => "UserGroup.Created";
    public string TargetType => "UserGroup";
    public string? GetTargetId(object? response) => response is Guid id ? id.ToString() : null;
    public string? GetMetadata(object? response) => System.Text.Json.JsonSerializer.Serialize(new { name = Name });
}


/// <summary>
/// Validates the <see cref="CreateUserGroupCommand"/>.
/// </summary>
public class CreateUserGroupCommandValidator : AbstractValidator<CreateUserGroupCommand>
{
    public CreateUserGroupCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
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
        if (!tenantContext.TenantId.HasValue) throw new InvalidOperationException(Normora.Shared.Constants.ApiMessages.TenantContextMissing);

        var exists = await dbContext.UserGroups
            .AnyAsync(g => g.TenantId == tenantContext.TenantId.Value && g.Name.ToLower() == request.Name.ToLower(), cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException(Normora.Shared.Constants.ApiMessages.UserGroupExists);
        }

        var group = new UserGroup
        {
            Id = Guid.NewGuid(),
            TenantId = tenantContext.TenantId.Value,
            Name = request.Name,
            Description = request.Description,
            // SEC-17: Only link departments that actually belong to this tenant
            UserGroupDepartments = request.DepartmentIds
                .Where(depId => dbContext.Departments
                    .Any(d => d.Id == depId && d.TenantId == tenantContext.TenantId.Value))
                .Select(depId => new UserGroupDepartment { DepartmentId = depId })
                .ToList()
        };

        dbContext.UserGroups.Add(group);
        await dbContext.SaveChangesAsync(cancellationToken);

        return group.Id;
    }
}
