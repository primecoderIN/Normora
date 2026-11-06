using FluentValidation;

namespace Normora.Modules.Tenants.Application.Departments.Commands;

/// <summary>
/// Validates the <see cref="CreateDepartmentCommand"/>.
/// </summary>
public class CreateDepartmentCommandValidator : AbstractValidator<CreateDepartmentCommand>
{
    public CreateDepartmentCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

