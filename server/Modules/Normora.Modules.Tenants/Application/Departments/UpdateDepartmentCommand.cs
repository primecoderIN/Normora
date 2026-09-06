using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Departments;

public record UpdateDepartmentCommand(Guid Id, string Name) : IRequest<bool>;

public class UpdateDepartmentCommandValidator : AbstractValidator<UpdateDepartmentCommand>
{
    public UpdateDepartmentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}

public class UpdateDepartmentCommandHandler(TenantsDbContext dbContext, ITenantContext tenantContext)
    : IRequestHandler<UpdateDepartmentCommand, bool>
{
    public async Task<bool> Handle(UpdateDepartmentCommand request, CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue) throw new InvalidOperationException("Tenant Context missing.");

        var department = await dbContext.Departments
            .FirstOrDefaultAsync(d => d.Id == request.Id && d.TenantId == tenantContext.TenantId.Value, cancellationToken);

        if (department == null)
        {
            return false;
        }

        // Enforce unique name per tenant (excluding self)
        var exists = await dbContext.Departments
            .AnyAsync(d => d.TenantId == tenantContext.TenantId.Value 
                           && d.Id != request.Id 
                           && d.Name.ToLower() == request.Name.ToLower(), cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException("A department with this name already exists.");
        }

        department.Name = request.Name;
        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
