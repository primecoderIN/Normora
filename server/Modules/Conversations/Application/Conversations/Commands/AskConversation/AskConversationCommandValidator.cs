using FluentValidation;

namespace Normora.Modules.Conversations.Application.Conversations.Commands;

/// <summary>
/// Validates <see cref="AskConversationCommand"/> to prevent empty questions, excessive prompt sizes,
/// or resource exhaustion on RAG vector retrieval limits.
/// </summary>
public sealed class AskConversationCommandValidator : AbstractValidator<AskConversationCommand>
{
    public AskConversationCommandValidator()
    {
        // Target Conversation Thread:
        // Must be a non-empty GUID referencing the active conversation.
        RuleFor(x => x.ConversationId)
            .NotEmpty();

        // User Prompt / Question:
        // Capped at 4000 characters to prevent token exhaustion and malicious payload amplification.
        RuleFor(x => x.Question)
            .NotEmpty()
            .MaximumLength(4000);

        // Retrieval Candidate Limit:
        // Restricted to between 1 and 20 candidates to prevent unbounded database and embedding memory consumption.
        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 20);
    }
}

