using FluentValidation;

namespace Normora.Modules.Tenants.Application.Users.Commands;

/// <summary>
/// Validates the <see cref="UpdateUserAssignmentsCommand"/>.
/// </summary>
public class UpdateUserAssignmentsCommandValidator : AbstractValidator<UpdateUserAssignmentsCommand>
{
    public UpdateUserAssignmentsCommandValidator()
    {
        // FluentValidation Pipeline Enforcement:
        // Evaluated automatically by ValidationBehavior<,> before UpdateUserAssignmentsCommandHandler executes.
        // - UserId: Target employee GUID to assign departments and user groups to.
        // - DepartmentIds: Collection of direct department GUIDs; must not be null.
        // - UserGroupIds: Collection of user group GUIDs; must not be null.
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.DepartmentIds)
            .NotNull();

        RuleFor(x => x.UserGroupIds)
            .NotNull();
    }
}

