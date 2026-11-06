using MediatR;
using Normora.Modules.Conversations.Application.Conversations.DTOs;

namespace Normora.Modules.Conversations.Application.Conversations.Commands;

/// <summary>Creates or reuses a secure public share token for a conversation owned by the caller.</summary>
public record ShareConversationCommand(Guid ConversationId) : IRequest<ConversationShareDto>;

