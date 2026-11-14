using FluentValidation;

namespace Normora.Modules.Conversations.Application.Approvals.Commands.ConfirmPendingApproval;

public sealed class ConfirmPendingApprovalCommandValidator : AbstractValidator<ConfirmPendingApprovalCommand>
{
    public ConfirmPendingApprovalCommandValidator()
    {
        RuleFor(x => x.ApprovalId).NotEmpty();
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}

