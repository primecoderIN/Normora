using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Conversations.Application.Conversations.DTOs;
using Normora.Modules.Conversations.Domain;
using Normora.Modules.Conversations.Persistence;
using Normora.Shared.Exceptions;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Application.Conversations.Queries;

public class GetConversationQueryHandler(
    ConversationsDbContext context,
    ICurrentUser currentUser,
    IMediator mediator) : IRequestHandler<GetConversationQuery, ConversationDetailDto>
{
    public async Task<ConversationDetailDto> Handle(GetConversationQuery request, CancellationToken cancellationToken)
    {
        var conversation = await context.Conversations
            .AsNoTracking()
            .Include(c => c.Messages)
            .ThenInclude(m => m.Citations)
            .Include(c => c.Messages)
            .ThenInclude(m => m.Feedbacks)
            .Where(c => c.Id == request.Id && c.UserId == currentUser.KeycloakUserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (conversation is null)
        {
            throw new BolaException();
        }

        // Identify citations that belong to inactive document versions
        var chunkIds = conversation.Messages.SelectMany(m => m.Citations.Select(c => c.DocumentChunkId)).Distinct().ToList();
        var outdatedChunkIds = chunkIds.Count > 0
            ? await mediator.Send(new Normora.Shared.Contracts.Documents.GetOutdatedChunkIdsQuery(chunkIds), cancellationToken)
            : [];

        return new ConversationDetailDto(
            conversation.Id,
            conversation.Title,
            conversation.Summary,
            conversation.CreatedAt,
            conversation.UpdatedAt,
            conversation.LastMessageAt,
            conversation.Messages
                .OrderBy(m => m.CreatedAt)
                .Select(m => new MessageDto(
                    m.Id,
                    m.Role,
                    m.Content,
                    m.CreatedAt,
                    m.Rewritten,
                    m.Citations.Select(c => new MessageCitationDto(
                        c.DocumentId,
                        c.DocumentChunkId,
                        c.FileName,
                        c.Score,
                        outdatedChunkIds.Contains(c.DocumentChunkId))).ToList(),
                    m.Feedbacks.FirstOrDefault(f => f.UserId == currentUser.KeycloakUserId) != null 
                        ? m.Feedbacks.FirstOrDefault(f => f.UserId == currentUser.KeycloakUserId)!.Rating 
                        : null
                )).ToList()
        );
    }
}
