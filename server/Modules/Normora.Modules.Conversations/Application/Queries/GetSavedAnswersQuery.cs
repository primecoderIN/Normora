using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Conversations.Application.Dtos;
using Normora.Modules.Conversations.Persistence;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Application.Queries;

/// <summary>
/// Returns all saved answers for the current user, ordered newest-first.
/// Includes the message content and source citations for display on the Saved Answers page.
/// </summary>
public record GetSavedAnswersQuery(int Limit = 50, int Offset = 0)
    : IRequest<IReadOnlyList<SavedAnswerDto>>;

public class GetSavedAnswersQueryHandler(
    ConversationsDbContext context,
    ICurrentUser currentUser) : IRequestHandler<GetSavedAnswersQuery, IReadOnlyList<SavedAnswerDto>>
{
    public async Task<IReadOnlyList<SavedAnswerDto>> Handle(
        GetSavedAnswersQuery request, CancellationToken cancellationToken)
    {
        // SEC-7: Clamp limit to a safe maximum to prevent resource exhaustion
        var limit = Math.Clamp(request.Limit, 1, 100);
        var offset = Math.Max(0, request.Offset);

        var answers = await context.SavedAnswers
            .AsNoTracking()
            .Include(s => s.Message)
            .ThenInclude(m => m.Citations)
            .Where(s => s.UserId == currentUser.KeycloakUserId)
            .OrderByDescending(s => s.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken);

        // Document Versioning (Phase 19): Identify citations that belong to inactive document versions.
        // We collect all chunk IDs from the fetched answers to verify their version status.
        var chunkIds = answers.SelectMany(a => a.Message.Citations.Select(c => c.DocumentChunkId)).Distinct().ToList();
        var outdatedChunkIds = new HashSet<Guid>();
        
        if (chunkIds.Any())
        {
            var idsStr = string.Join(",", chunkIds.Select(id => $"'{id}'"));
            
            // Raw SQL query to maintain module independence while joining the 'documents' schema.
            // Checks if any of the document chunks belong to a DocumentVersion where IsActive = false.
            var sql = $@"
                SELECT c.""Id"" 
                FROM documents.""DocumentChunks"" c
                JOIN documents.""DocumentVersions"" v ON c.""DocumentVersionId"" = v.""Id""
                WHERE v.""IsActive"" = false AND c.""Id"" IN ({idsStr})";
                
            var results = await context.Database.SqlQueryRaw<Guid>(sql).ToListAsync(cancellationToken);
            foreach (var id in results)
            {
                outdatedChunkIds.Add(id);
            }
        }

        return answers.Select(s => new SavedAnswerDto(
            s.Id,
            s.MessageId,
            s.ConversationId,
            s.Message.Content,
            s.Message.Citations
                .Select(c => new CitationDto(c.DocumentId, c.FileName, c.Score, outdatedChunkIds.Contains(c.DocumentChunkId)))
                .ToList(),
            s.CreatedAt)).ToList();
    }
}
