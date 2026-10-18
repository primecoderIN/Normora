using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Domain;

/// <summary>
/// Represents a time-limited or permanent public share link for a conversation.
/// Allows external or guest viewing with read-only access scoped by a secure slug.
/// </summary>
public class ConversationShare : ITenantEntity
{
    public Guid Id { get; set; }

    /// <summary>Tenant identifier ensuring share records remain isolated.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Target conversation being shared.</summary>
    public Guid ConversationId { get; set; }

    /// <summary>User who generated this share link.</summary>
    public string CreatedByUserId { get; set; } = string.Empty;

    /// <summary>Randomized 16-character base62 slug used in the share URL.</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>Optional expiry date after which access returns 404.</summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Conversation Conversation { get; set; } = null!;
}

