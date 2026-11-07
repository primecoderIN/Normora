using FluentValidation;

namespace Normora.Modules.Tenants.Application.UserGroups.Commands;

/// <summary>
/// Validates the <see cref="UpdateUserGroupAssignmentsCommand"/>.
/// </summary>
public class UpdateUserGroupAssignmentsCommandValidator : AbstractValidator<UpdateUserGroupAssignmentsCommand>
{
    public UpdateUserGroupAssignmentsCommandValidator()
    {
        // FluentValidation Pipeline Enforcement:
        // Evaluated automatically by ValidationBehavior<,> before UpdateUserGroupAssignmentsCommandHandler executes.
        // - Id: Required GUID of the user group.
        // - MemberUserIds: Collection of user IDs to associate with this group; must not be null.
        // - DepartmentIds: Collection of department IDs to link to this group; must not be null.
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.MemberUserIds)
            .NotNull();

        RuleFor(x => x.DepartmentIds)
            .NotNull();
    }
}

