using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Domain;

public class Message : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public Guid TenantId { get; set; }
    public MessageRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public int? TokenCount { get; set; }
    
    public bool RequiresContext { get; set; }
    public bool Rewritten { get; set; }
    public string? RetrievalQuery { get; set; }
    public string? ProcessingMetadata { get; set; }

    public Conversation Conversation { get; set; } = null!;
    public ICollection<MessageCitation> Citations { get; set; } = new List<MessageCitation>();
}

public enum MessageRole
{
    User = 0,
    Assistant = 1,
    System = 2
}
