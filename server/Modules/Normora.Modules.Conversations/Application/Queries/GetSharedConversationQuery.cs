using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Conversations.Domain;
using Normora.Modules.Conversations.Persistence;
using Normora.Shared.Exceptions;
using Normora.Shared.Interfaces;
using Npgsql;

namespace Normora.Modules.Conversations.Application.Queries;

public record GetSharedConversationQuery(string Token) : IRequest<SharedConversationDto>;

public record SharedConversationDto(
    Guid Id,
    string Title,
    string? Summary,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset LastMessageAt,
    IReadOnlyList<MessageDto> Messages,
    string? SharedByName);

public class GetSharedConversationQueryHandler(
    ConversationsDbContext context,
    ITenantContext tenantContext) : IRequestHandler<GetSharedConversationQuery, SharedConversationDto>
{
    public async Task<SharedConversationDto> Handle(GetSharedConversationQuery request, CancellationToken cancellationToken)
    {
        // 1. Look up the share record by token, bypassing default tenant filter for token verification
        var share = await context.ConversationShares
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Token == request.Token, cancellationToken);

        if (share is null)
        {
            throw new BolaException();
        }

        // 2. Check expiration (returns 404 to prevent link enumeration)
        if (share.ExpiresAt.HasValue && share.ExpiresAt.Value <= DateTimeOffset.UtcNow)
        {
            throw new BolaException();
        }

        // 3. Strict tenant isolation: recipient must be in the same workspace tenant
        if (!tenantContext.IsTenantResolved || !tenantContext.TenantId.HasValue || share.TenantId != tenantContext.TenantId.Value)
        {
            throw new BolaException();
        }

        // 4. Load the conversation and messages
        var conversation = await context.Conversations
            .AsNoTracking()
            .Include(c => c.Messages)
            .ThenInclude(m => m.Citations)
            .FirstOrDefaultAsync(c => c.Id == share.ConversationId && c.TenantId == share.TenantId, cancellationToken);

        if (conversation is null)
        {
            throw new BolaException();
        }

        // 5. Document Versioning check: identify citations from outdated versions
        var chunkIds = conversation.Messages.SelectMany(m => m.Citations.Select(c => c.DocumentChunkId)).Distinct().ToList();
        var outdatedChunkIds = new HashSet<Guid>();

        if (chunkIds.Count > 0)
        {
            var sql = @"
                SELECT c.""Id"" 
                FROM ""DocumentChunks"" c
                JOIN ""DocumentVersions"" v ON c.""DocumentVersionId"" = v.""Id""
                WHERE v.""IsActive"" = false AND c.""Id"" = ANY(@chunkIds)";

            var param = new NpgsqlParameter("chunkIds", chunkIds.ToArray());
            var results = await context.Database.SqlQueryRaw<Guid>(sql, param).ToListAsync(cancellationToken);
            foreach (var id in results)
            {
                outdatedChunkIds.Add(id);
            }
        }

        // 6. Resolve the sharer's display name from Users
        string? sharedByName = null;
        try
        {
            var userSql = @"SELECT COALESCE(""DisplayName"", ""Email"", 'A team member') FROM ""Users"" WHERE ""KeycloakUserId"" = @userId LIMIT 1";
            var userParam = new NpgsqlParameter("userId", share.CreatedByUserId);
            sharedByName = await context.Database.SqlQueryRaw<string>(userSql, userParam).FirstOrDefaultAsync(cancellationToken);
        }
        catch
        {
            sharedByName = "A team member";
        }

        return new SharedConversationDto(
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
                    null
                )).ToList(),
            sharedByName
        );
    }
}
