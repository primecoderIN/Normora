using FluentValidation;

namespace Normora.Modules.Tenants.Application.Departments.Commands;

/// <summary>
/// Validates the <see cref="UpdateDepartmentCommand"/>.
/// </summary>
public class UpdateDepartmentCommandValidator : AbstractValidator<UpdateDepartmentCommand>
{
    public UpdateDepartmentCommandValidator()
    {
        // FluentValidation Pipeline Enforcement:
        // Evaluated automatically by ValidationBehavior<,> before UpdateDepartmentCommandHandler executes.
        // - Id: Required non-empty GUID representing the target department.
        // - Name: Mandatory, non-empty, and capped at 100 characters.
        // - Description: Optional metadata capped at 500 characters.
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(500);
    }
}

