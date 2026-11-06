using FluentValidation;

namespace Normora.Modules.Tenants.Application.UserGroups.Commands;

/// <summary>
/// Validates the <see cref="CreateUserGroupCommand"/>.
/// </summary>
public class CreateUserGroupCommandValidator : AbstractValidator<CreateUserGroupCommand>
{
    public CreateUserGroupCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.DepartmentIds).NotNull();
    }
}

