using FluentValidation;

namespace Normora.Modules.Tenants.Application.UserGroups.Commands;

/// <summary>
/// Validates the <see cref="CreateUserGroupCommand"/>.
/// </summary>
public class CreateUserGroupCommandValidator : AbstractValidator<CreateUserGroupCommand>
{
    public CreateUserGroupCommandValidator()
    {
        // FluentValidation Pipeline Enforcement:
        // Evaluated automatically by ValidationBehavior<,> before CreateUserGroupCommandHandler executes.
        // - Name: Mandatory, non-empty, and capped at 100 characters.
        // - Description: Optional metadata capped at 500 characters.
        // - DepartmentIds: Non-null collection (empty is valid if group has no immediate department bindings).
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(500);

        RuleFor(x => x.DepartmentIds)
            .NotNull();
    }
}

