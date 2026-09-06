using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Domain;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Departments;

/// <summary>
/// Command to create a new department within the current tenant.
/// </summary>
/// <param name="Name">The name of the new department.</param>
public record CreateDepartmentCommand(string Name) : IRequest<Guid>;

/// <summary>
/// Validates the <see cref="CreateDepartmentCommand"/>.
/// </summary>
public class CreateDepartmentCommandValidator : AbstractValidator<CreateDepartmentCommand>
{
    public CreateDepartmentCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}

/// <summary>
/// Handles the <see cref="CreateDepartmentCommand"/> by ensuring name uniqueness and saving the new department.
/// </summary>
public class CreateDepartmentCommandHandler(TenantsDbContext dbContext, ITenantContext tenantContext)
    : IRequestHandler<CreateDepartmentCommand, Guid>
{
    public async Task<Guid> Handle(CreateDepartmentCommand request, CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue) throw new InvalidOperationException("Tenant Context missing.");

        // Enforce unique name per tenant
        var exists = await dbContext.Departments
            .AnyAsync(d => d.TenantId == tenantContext.TenantId.Value && d.Name.ToLower() == request.Name.ToLower(), cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException("A department with this name already exists.");
        }

        var department = new Department
        {
            Id = Guid.NewGuid(),
            TenantId = tenantContext.TenantId.Value,
            Name = request.Name
        };

        dbContext.Departments.Add(department);
        await dbContext.SaveChangesAsync(cancellationToken);

        return department.Id;
    }
}
