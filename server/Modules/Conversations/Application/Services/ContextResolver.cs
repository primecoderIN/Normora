using Microsoft.EntityFrameworkCore;
using Normora.Modules.Conversations.Persistence;

namespace Normora.Modules.Conversations.Application.Services;

public class ContextResolver(ConversationsDbContext context) : IContextResolver
{
    public async Task<IReadOnlyList<ConversationMessageContext>> GetConversationContextAsync(
        Guid conversationId,
        string userId,
        int messageLimit = 5,
        CancellationToken cancellationToken = default)
    {
        var conversation = await context.Conversations
            .AsNoTracking()
            .Where(c => c.Id == conversationId && c.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken);

        if (conversation is null)
            return Array.Empty<ConversationMessageContext>();

        var messages = await context.Messages
            .AsNoTracking()
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(messageLimit)
            .ToListAsync(cancellationToken);

        return messages
            .OrderBy(m => m.CreatedAt) // chronological order
            .Select(m => new ConversationMessageContext(
                m.Role.ToString(),
                m.Content))
            .ToList();
    }
}
