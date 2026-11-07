using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Domain;

/// <summary>
/// Root Aggregate Entity for Conversational Sessions.
/// Represents a multi-turn chat thread between an employee and the conversational RAG assistant.
/// Invariants & Architecture:
/// 1. Data Isolation: Partitioned by both TenantId (ambient workspace) and UserId (OIDC ExternalUserId).
/// 2. Rolling Context Summarization: Long-running threads maintain a rolling Summary and monotonic
///    SummaryVersion counter so the LLM retains conversation context beyond the token window.
/// 3. Ordering: LastMessageAt is updated on every turn to support keyset sorting and responsive sidebar rosters.
/// </summary>
public class Conversation : ITenantEntity
{
    public Guid Id { get; set; }

    /// <summary>Tenant workspace identifier for strict logical data isolation.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Subject identifier (ExternalUserId) of the user who authoritatively owns this conversation.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Human-readable title, generated asynchronously by AutoTitleService or edited by user.</summary>
    public string Title { get; set; } = "New conversation";

    /// <summary>Rolling summary used for long-term multi-turn context retention.</summary>
    public string? Summary { get; set; }

    /// <summary>Monotonically increasing version counter for optimistic concurrency and caching.</summary>
    public int SummaryVersion { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Timestamp of the most recent message in the thread, used for sorting list views.</summary>
    public DateTimeOffset LastMessageAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Ordered messages belonging to this conversation thread.</summary>
    public ICollection<Message> Messages { get; set; } = new List<Message>();

    public void UpdateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return;
        Title = title.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateSummary(string summary)
    {
        // Concurrency Invariant:
        // Monotonically increments SummaryVersion whenever summary state transitions.
        Summary = summary;
        SummaryVersion++;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void TouchLastMessage(DateTimeOffset? timestamp = null)
    {
        var now = timestamp ?? DateTimeOffset.UtcNow;
        LastMessageAt = now;
        UpdatedAt = now;
    }
}

