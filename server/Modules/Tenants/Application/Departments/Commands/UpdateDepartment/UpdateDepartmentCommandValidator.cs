using FluentValidation;

namespace Normora.Modules.Tenants.Application.Departments.Commands;

/// <summary>
/// Validates the <see cref="UpdateDepartmentCommand"/>.
/// </summary>
public class UpdateDepartmentCommandValidator : AbstractValidator<UpdateDepartmentCommand>
{
    public UpdateDepartmentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

