using FluentValidation;

namespace Normora.Modules.Conversations.Application.Approvals.Commands.CreatePendingApproval;

public sealed class CreatePendingApprovalCommandValidator : AbstractValidator<CreatePendingApprovalCommand>
{
    public CreatePendingApprovalCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.ToolName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ArgumentsJson).NotEmpty();
    }
}

