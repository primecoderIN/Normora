using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Domain;

public class ConversationShare : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ConversationId { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty; // 16-char base62 slug
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Conversation Conversation { get; set; } = null!;
}
