using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Domain;

/// <summary>
/// Represents an assistant message that an employee has bookmarked for later reference.
/// A user can save any assistant message from any of their conversations.
/// </summary>
public class SavedAnswer : ITenantEntity
{
    public Guid Id { get; set; }

    /// <summary>The tenant this saved answer belongs to.</summary>
    public Guid TenantId { get; set; }

    /// <summary>The Keycloak user ID of the employee who saved this answer.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>The assistant message that was saved.</summary>
    public Guid MessageId { get; set; }

    /// <summary>The conversation the message belongs to (for navigation).</summary>
    public Guid ConversationId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public Message Message { get; set; } = null!;
}
