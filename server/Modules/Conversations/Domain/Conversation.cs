using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Domain;

/// <summary>
/// Represents a multi-turn conversation session between a tenant user and the RAG assistant.
/// Maintains thread metadata, rolling summary state, and tenant boundaries.
/// </summary>
public class Conversation : ITenantEntity
{
    public Guid Id { get; set; }

    /// <summary>Tenant identifier for strict logical data isolation.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Subject identifier (Keycloak sub) of the user who owns this conversation.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Human-readable title, generated automatically or updated by the user.</summary>
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
}

