using Microsoft.EntityFrameworkCore;
using Normora.Modules.Conversations.Persistence;

namespace Normora.Modules.Conversations.Application.Services;

/// <summary>
/// Resolves recent conversation history to provide sliding multi-turn context to LLM query rewrites.
/// </summary>
public class ContextResolver(ConversationsDbContext context) : IContextResolver
{
    /// <summary>
    /// Fetches recent conversation turns scoped to the user, ordering them chronologically for prompt assembly.
    /// </summary>
    public async Task<IReadOnlyList<ConversationMessageContext>> GetConversationContextAsync(
        Guid conversationId,
        string userId,
        int messageLimit = 5,
        CancellationToken cancellationToken = default)
    {
        // Verify conversation ownership to enforce security boundaries
        var conversation = await context.Conversations
            .AsNoTracking()
            .Where(c => c.Id == conversationId && c.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken);

        if (conversation is null)
            return Array.Empty<ConversationMessageContext>();

        // Query the latest N messages in descending order, then reorder chronologically
        var messages = await context.Messages
            .AsNoTracking()
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(messageLimit)
            .ToListAsync(cancellationToken);

        return messages
            .OrderBy(m => m.CreatedAt)
            .Select(m => new ConversationMessageContext(
                m.Role.ToString(),
                m.Content))
            .ToList();
    }
}

