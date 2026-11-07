using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Domain;

/// <summary>
/// Domain Entity representing a bookmarked assistant response.
/// Allows employees to preserve valuable AI-generated answers, citations, and export them
/// to Markdown, PDF, or Word documents.
/// Invariants:
/// - Partitioned by TenantId and UserId (ExternalUserId).
/// - Links to MessageId (only assistant messages can be bookmarked).
/// </summary>
public class SavedAnswer : ITenantEntity
{
    public Guid Id { get; set; }

    /// <summary>The tenant workspace this saved answer belongs to.</summary>
    public Guid TenantId { get; set; }

    /// <summary>The OIDC subject identifier (ExternalUserId) of the employee who saved this answer.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>The assistant message that was saved.</summary>
    public Guid MessageId { get; set; }

    /// <summary>The conversation the message belongs to (for navigation).</summary>
    public Guid ConversationId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public Message Message { get; set; } = null!;
}
