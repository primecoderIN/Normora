using FluentValidation;

namespace Normora.Modules.Tenants.Application.UserGroups.Commands;

/// <summary>
/// Validates the <see cref="UpdateUserGroupAssignmentsCommand"/>.
/// </summary>
public class UpdateUserGroupAssignmentsCommandValidator : AbstractValidator<UpdateUserGroupAssignmentsCommand>
{
    public UpdateUserGroupAssignmentsCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.MemberUserIds).NotNull();
        RuleFor(x => x.DepartmentIds).NotNull();
    }
}

