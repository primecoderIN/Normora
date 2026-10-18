using FluentValidation;

namespace Normora.Modules.Conversations.Application.Feedback;

/// <summary>
/// Validates user feedback submissions ensuring valid identifiers, defined enum values, and length limits.
/// </summary>
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

