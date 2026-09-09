using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Conversations.Application.Dtos;
using Normora.Modules.Conversations.Domain;
using Normora.Modules.Conversations.Persistence;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Application.Queries;

public record GetConversationQuery(Guid Id) : IRequest<ConversationDetailDto?>;

public record ConversationDetailDto(
    Guid Id,
    string Title,
    string? Summary,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset LastMessageAt,
    IReadOnlyList<MessageDto> Messages);

public record MessageDto(
    Guid Id,
    MessageRole Role,
    string Content,
    DateTimeOffset CreatedAt,
    bool Rewritten,
    IReadOnlyList<MessageCitationDto> Citations);

public record MessageCitationDto(
    Guid DocumentId,
    Guid DocumentChunkId,
    string FileName,
    double Score);

public class GetConversationQueryHandler(
    ConversationsDbContext context,
    ICurrentUser currentUser) : IRequestHandler<GetConversationQuery, ConversationDetailDto?>
{
    public async Task<ConversationDetailDto?> Handle(GetConversationQuery request, CancellationToken cancellationToken)
    {
        var conversation = await context.Conversations
            .AsNoTracking()
            .Include(c => c.Messages)
            .ThenInclude(m => m.Citations)
            .Where(c => c.Id == request.Id && c.UserId == currentUser.KeycloakUserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (conversation is null)
        {
            return null;
        }

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
                        c.Score)).ToList()
                )).ToList()
        );
    }
}
