using FluentValidation;

namespace Normora.Modules.Tenants.Application.Departments.Commands;

/// <summary>
/// Validates the <see cref="CreateDepartmentCommand"/>.
/// </summary>
public class CreateDepartmentCommandValidator : AbstractValidator<CreateDepartmentCommand>
{
    public CreateDepartmentCommandValidator()
    {
        // FluentValidation Pipeline Enforcement:
        // Evaluated automatically by ValidationBehavior<,> before CreateDepartmentCommandHandler executes.
        // - Name: Mandatory, capped at 100 characters to prevent database column truncation and UI overflow.
        // - Description: Optional metadata capped at 500 characters.
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(500);
    }
}

