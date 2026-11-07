using FluentValidation;

namespace Normora.Modules.Conversations.Application.Conversations.Commands;

/// <summary>
/// Validates user feedback submissions ensuring valid identifiers, defined enum values, and length limits.
/// </summary>
public sealed class SubmitFeedbackCommandValidator : AbstractValidator<SubmitFeedbackCommand>
{
    public SubmitFeedbackCommandValidator()
    {
        // FluentValidation Pipeline Enforcement:
        // Evaluated automatically by ValidationBehavior<,> before SubmitFeedbackCommandHandler executes.
        // - ConversationId & MessageId: Required GUID identifiers.
        // - Rating: Must be a valid defined MessageFeedbackRating enum value (Positive / Negative).
        // - Comment: Optional feedback rationale capped at 500 characters.
        RuleFor(x => x.ConversationId).NotEmpty();
        RuleFor(x => x.MessageId).NotEmpty();
        RuleFor(x => x.Rating).IsInEnum();
        RuleFor(x => x.Comment).MaximumLength(500);
    }
}

