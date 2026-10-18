using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Domain;

/// <summary>
/// Represents a single message exchange within a conversation thread.
/// Tracks query rewriting, context requirements, token usage, and retrieval citations.
/// </summary>
public class Message : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }

    /// <summary>Tenant identifier for strict tenant boundary isolation.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Indicates whether the message originated from user, assistant, or system.</summary>
    public MessageRole Role { get; set; }

    /// <summary>Message body text (prompt or generated answer).</summary>
    public string Content { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Estimated or actual token count consumed by this message.</summary>
    public int? TokenCount { get; set; }
    
    /// <summary>True if the query classifier determined enterprise document context is required.</summary>
    public bool RequiresContext { get; set; }

    /// <summary>True if the user query was rewritten by the LLM query rewriter for better retrieval.</summary>
    public bool Rewritten { get; set; }

    /// <summary>The rewritten or expanded query executed against Qdrant/Postgres vector storage.</summary>
    public string? RetrievalQuery { get; set; }

    /// <summary>JSON serialized performance and latency telemetry (durations, rerank times).</summary>
    public string? ProcessingMetadata { get; set; }

    public Conversation Conversation { get; set; } = null!;

    /// <summary>Source citations supporting the claims in this assistant message.</summary>
    public ICollection<MessageCitation> Citations { get; set; } = new List<MessageCitation>();

    /// <summary>User ratings and feedback submissions for this message.</summary>
    public ICollection<MessageFeedback> Feedbacks { get; set; } = new List<MessageFeedback>();
}

/// <summary>
/// Sender role within the conversation flow.
/// </summary>
public enum MessageRole
{
    User = 0,
    Assistant = 1,
    System = 2
}

