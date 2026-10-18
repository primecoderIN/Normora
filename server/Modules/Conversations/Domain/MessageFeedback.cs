using System;

namespace Normora.Modules.Conversations.Domain;

/// <summary>
/// User feedback record for a specific assistant message.
/// Used for quality evaluation, response auditing, and continuous RAG prompt/index tuning.
/// </summary>
public sealed class MessageFeedback : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>Tenant identifier for strict tenant boundary isolation.</summary>
    public Guid TenantId { get; set; }
    
    /// <summary>Subject identifier (Keycloak sub) of the user providing feedback.</summary>
    public string UserId { get; set; } = null!;
    
    /// <summary>Identifier of the rated message.</summary>
    public Guid MessageId { get; set; }
    public Message Message { get; set; } = null!;
    
    /// <summary>Positive (thumbs up) or negative (thumbs down) rating.</summary>
    public MessageFeedbackRating Rating { get; set; }
    
    /// <summary>Optional text comments detailing why the response was helpful or unhelpful.</summary>
    public string? Comment { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

