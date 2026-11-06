using FluentValidation;

namespace Normora.Modules.Tenants.Application.Users.Commands;

/// <summary>
/// Validates the <see cref="UpdateUserAssignmentsCommand"/>.
/// </summary>
public class UpdateUserAssignmentsCommandValidator : AbstractValidator<UpdateUserAssignmentsCommand>
{
    public UpdateUserAssignmentsCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.DepartmentIds).NotNull();
        RuleFor(x => x.UserGroupIds).NotNull();
    }
}

