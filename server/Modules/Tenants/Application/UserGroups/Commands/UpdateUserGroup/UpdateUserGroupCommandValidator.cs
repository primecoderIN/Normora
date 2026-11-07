using FluentValidation;

namespace Normora.Modules.Tenants.Application.UserGroups.Commands;

/// <summary>
/// Validates the <see cref="UpdateUserGroupCommand"/>.
/// </summary>
public class UpdateUserGroupCommandValidator : AbstractValidator<UpdateUserGroupCommand>
{
    public UpdateUserGroupCommandValidator()
    {
        // FluentValidation Pipeline Enforcement:
        // Evaluated automatically by ValidationBehavior<,> before UpdateUserGroupCommandHandler executes.
        // - Id: Required GUID of the group to update.
        // - Name: Mandatory, non-empty, and capped at 100 characters.
        // - Description: Optional metadata capped at 500 characters.
        // - DepartmentIds: Collection of department GUIDs to synchronize; must not be null.
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(500);

        RuleFor(x => x.DepartmentIds)
            .NotNull();
    }
}

