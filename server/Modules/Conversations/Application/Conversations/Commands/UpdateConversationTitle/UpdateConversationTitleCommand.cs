using MediatR;

namespace Normora.Modules.Conversations.Application.Conversations.Commands;

/// <summary>
/// Command to update the title of a conversation during first-turn auto-titling.
/// </summary>
public sealed record UpdateConversationTitleCommand(
    Guid ConversationId,
    string Title) : IRequest<bool>;

