using MediatR;
using Normora.Modules.Conversations.Application.Conversations.DTOs;

namespace Normora.Modules.Conversations.Application.Conversations.Queries;

/// <summary>Resolves a public share token while enforcing its expiration and visibility rules.</summary>
public record GetSharedConversationQuery(string Token) : IRequest<SharedConversationDto>;

