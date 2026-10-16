using FluentValidation;

namespace Normora.Modules.Conversations.Application.Feedback;

public sealed class SubmitFeedbackCommandValidator : AbstractValidator<SubmitFeedbackCommand>
{
    public SubmitFeedbackCommandValidator()
    {
        RuleFor(x => x.ConversationId).NotEmpty();
        RuleFor(x => x.MessageId).NotEmpty();
        RuleFor(x => x.Rating).IsInEnum();
        RuleFor(x => x.Comment).MaximumLength(500);
    }
}
