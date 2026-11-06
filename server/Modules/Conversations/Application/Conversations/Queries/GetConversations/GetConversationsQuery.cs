using MediatR;
using Normora.Modules.Conversations.Application.Conversations.DTOs;

namespace Normora.Modules.Conversations.Application.Conversations.Queries;

/// <summary>Returns a bounded, paginated list of conversations for the current tenant and user.</summary>
public record GetConversationsQuery(int Limit = 50, int Offset = 0) : IRequest<IReadOnlyList<ConversationDto>>;

