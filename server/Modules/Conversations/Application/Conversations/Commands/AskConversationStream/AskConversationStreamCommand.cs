using MediatR;
using Normora.Modules.Conversations.Application.Conversations.DTOs;

namespace Normora.Modules.Conversations.Application.Conversations.Commands;

/// <summary>
/// Streaming variant of <see cref="AskConversationCommand"/>. Same RAG pipeline but yields
/// <see cref="AskConversationStreamEvent"/> instances via SSE instead of returning a single result.
/// Handled by <see cref="AskConversationStreamCommandHandler"/>.
/// </summary>
public sealed record AskConversationStreamCommand(
    Guid ConversationId,
    string Question,
    int Limit = 5) : IStreamRequest<AskConversationStreamEvent>;
