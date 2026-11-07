using MediatR;
using Normora.Modules.Conversations.Application.Conversations.DTOs;

namespace Normora.Modules.Conversations.Application.Conversations.Commands;

/// <summary>
/// Command to send a message within a conversation and receive a grounded, context-aware RAG answer.
/// Handled by <see cref="AskConversationCommandHandler"/>.
/// </summary>
public sealed record AskConversationCommand(
    Guid ConversationId,
    string Question,
    int Limit = 5) : IRequest<AskConversationResult>;

