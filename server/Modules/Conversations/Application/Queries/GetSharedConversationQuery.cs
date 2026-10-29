using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Conversations.Domain;
using Normora.Modules.Conversations.Persistence;
using Normora.Shared.Exceptions;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Application.Queries;

/// <summary>Resolves a public share token while enforcing its expiration and visibility rules.</summary>
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
    ITenantContext tenantContext,
    IMediator mediator) : IRequestHandler<GetSharedConversationQuery, SharedConversationDto>
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
        var outdatedChunkIds = chunkIds.Count > 0
            ? await mediator.Send(new Normora.Shared.Contracts.Documents.GetOutdatedChunkIdsQuery(chunkIds), cancellationToken)
            : [];

        // 6. Resolve the sharer's display name from Users
        var sharedByName = await mediator.Send(new Normora.Shared.Contracts.Tenants.GetUserDisplayNameQuery(share.CreatedByUserId), cancellationToken)
            ?? "A team member";

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
