using FluentValidation;
using Normora.Shared.Constants;

namespace Normora.Modules.Tenants.Application.Tenants.Commands;

/// <summary>
/// Validates <see cref="CreateTenantCommand"/> inputs prior to provisioning a new workspace tenant.
/// Enforces slug format and length restrictions to prevent routing collisions and path traversal attacks.
/// </summary>
public sealed class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
    public CreateTenantCommandValidator()
    {
        // Organization Display Name:
        // Must be non-empty and bounded to 100 characters to prevent database truncation or UI overflow.
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        // URL-Safe Workspace Slug:
        // Must be between 3 and 50 characters, lower-case alphanumeric with hyphen separators.
        // Rejects path-traversal sequences, symbols, and uppercase characters to ensure safe subdomain/path routing.
        RuleFor(x => x.Slug)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(50)
            .Matches(@"^[a-z0-9]+(-[a-z0-9]+)*$")
            .WithMessage(ApiMessages.InvalidSlug);
    }
}

