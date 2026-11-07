using MediatR;

namespace Normora.Modules.Conversations.Application.Conversations.Commands;

/// <summary>
/// Command to update the display title of a conversation during background first-turn auto-titling.
/// Handled by <see cref="UpdateConversationTitleCommandHandler"/>.
/// </summary>
/// <remarks>
/// First-Turn Titling Invariant:
/// - Only modifies conversations whose title is currently the default placeholder ("New Conversation").
/// - Protects user-customized titles from being accidentally overwritten.
/// </remarks>
/// <param name="ConversationId">The target conversation thread identifier.</param>
/// <param name="Title">The generated 3-6 word summary title.</param>
public sealed record UpdateConversationTitleCommand(
    Guid ConversationId,
    string Title) : IRequest<bool>;

