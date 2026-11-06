using MediatR;
using Normora.Modules.Conversations.Application.Conversations.DTOs;

namespace Normora.Modules.Conversations.Application.Conversations.Queries;

/// <summary>Loads one tenant-scoped conversation with its ordered messages and citations.</summary>
public record GetConversationQuery(Guid Id) : IRequest<ConversationDetailDto>;

