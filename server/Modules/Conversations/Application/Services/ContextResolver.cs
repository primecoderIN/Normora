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
        // BOLA ownership verification:
        // Ensure the conversation belongs to the authenticated external user ID before reading chat turns.
        var conversation = await context.Conversations
            .AsNoTracking()
            .Where(c => c.Id == conversationId && c.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken);

        if (conversation is null)
            return Array.Empty<ConversationMessageContext>();

        // Two-phase chronological ordering:
        // First sort descending to retrieve the N most recent message turns efficiently via index,
        // then sort ascending in memory so the LLM receives the natural, chronological flow of dialogue.
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

