using FluentValidation;

namespace Normora.Modules.Conversations.Application.Approvals.Commands.DismissPendingApproval;

public sealed class DismissPendingApprovalCommandValidator : AbstractValidator<DismissPendingApprovalCommand>
{
    public DismissPendingApprovalCommandValidator()
    {
        RuleFor(x => x.ApprovalId).NotEmpty();
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}

