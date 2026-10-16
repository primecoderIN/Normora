using System;

namespace Normora.Modules.Conversations.Domain;

public sealed class MessageFeedback : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid TenantId { get; set; }
    
    public string UserId { get; set; } = null!;
    
    public Guid MessageId { get; set; }
    public Message Message { get; set; } = null!;
    
    public MessageFeedbackRating Rating { get; set; }
    
    public string? Comment { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
