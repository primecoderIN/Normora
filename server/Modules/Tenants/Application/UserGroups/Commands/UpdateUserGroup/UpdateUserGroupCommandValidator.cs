using FluentValidation;

namespace Normora.Modules.Tenants.Application.UserGroups.Commands;

/// <summary>
/// Validates the <see cref="UpdateUserGroupCommand"/>.
/// </summary>
public class UpdateUserGroupCommandValidator : AbstractValidator<UpdateUserGroupCommand>
{
    public UpdateUserGroupCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.DepartmentIds).NotNull();
    }
}

