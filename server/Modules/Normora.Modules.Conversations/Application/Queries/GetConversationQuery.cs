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
    IReadOnlyList<MessageCitationDto> Citations,
    MessageFeedbackRating? Feedback);

public record MessageCitationDto(
    Guid DocumentId,
    Guid DocumentChunkId,
    string FileName,
    double Score,
    bool IsOutdated);

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
            .Include(c => c.Messages)
            .ThenInclude(m => m.Feedbacks)
            .Where(c => c.Id == request.Id && c.UserId == currentUser.KeycloakUserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (conversation is null)
        {
            throw new Normora.Shared.Exceptions.BolaException();
        }

        // Document Versioning (Phase 19): Identify citations that belong to inactive document versions.
        var chunkIds = conversation.Messages.SelectMany(m => m.Citations.Select(c => c.DocumentChunkId)).Distinct().ToList();
        var outdatedChunkIds = new HashSet<Guid>();
        
        if (chunkIds.Any())
        {
            var sql = $@"
                SELECT c.""Id"" 
                FROM ""DocumentChunks"" c
                JOIN ""DocumentVersions"" v ON c.""DocumentVersionId"" = v.""Id""
                WHERE v.""IsActive"" = false AND c.""Id"" = ANY(@chunkIds)";

            var param = new Npgsql.NpgsqlParameter("chunkIds", chunkIds.ToArray());
            var results = await context.Database.SqlQueryRaw<Guid>(sql, param).ToListAsync(cancellationToken);
            foreach (var id in results)
            {
                outdatedChunkIds.Add(id);
            }
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
                        c.Score,
                        outdatedChunkIds.Contains(c.DocumentChunkId))).ToList(),
                    m.Feedbacks.FirstOrDefault(f => f.UserId == currentUser.KeycloakUserId) != null 
                        ? m.Feedbacks.FirstOrDefault(f => f.UserId == currentUser.KeycloakUserId)!.Rating 
                        : null
                )).ToList()
        );
    }
}
