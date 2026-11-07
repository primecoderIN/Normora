using FluentValidation;

namespace Normora.Modules.Tenants.Application.Invitations.Commands;

/// <summary>
/// Validates <see cref="InviteEmployeeCommand"/> inputs prior to issuing workspace invitation tokens.
/// Enforces valid corporate email structure and size constraints.
/// </summary>
public sealed class InviteEmployeeCommandValidator : AbstractValidator<InviteEmployeeCommand>
{
    public InviteEmployeeCommandValidator()
    {
        // Corporate Email Validation:
        // Must be non-empty, conform to standard RFC email format, and stay within standard 255 character limit.
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(255);
    }
}

