using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Conversations.Application.Dtos;
using Normora.Modules.Conversations.Persistence;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Application.Queries;

public record GetConversationsQuery(int Limit = 50, int Offset = 0) : IRequest<IReadOnlyList<ConversationDto>>;

public class GetConversationsQueryHandler(
    ConversationsDbContext context,
    ICurrentUser currentUser) : IRequestHandler<GetConversationsQuery, IReadOnlyList<ConversationDto>>
{
    public async Task<IReadOnlyList<ConversationDto>> Handle(GetConversationsQuery request, CancellationToken cancellationToken)
    {
        // SEC-7: Clamp limit to a safe maximum to prevent resource exhaustion via huge DB scans
        var limit = Math.Clamp(request.Limit, 1, 100);
        var offset = Math.Max(0, request.Offset);

        return await context.Conversations
            .AsNoTracking()
            .Where(c => c.UserId == currentUser.KeycloakUserId)
            .OrderByDescending(c => c.LastMessageAt)
            .Skip(offset)
            .Take(limit)
            .Select(c => new ConversationDto(
                c.Id,
                c.Title,
                c.Summary,
                c.CreatedAt,
                c.UpdatedAt,
                c.LastMessageAt))
            .ToListAsync(cancellationToken);
    }
}
