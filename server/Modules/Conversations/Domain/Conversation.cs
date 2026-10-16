using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Domain;

public class Conversation : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = "New conversation";
    public string? Summary { get; set; }
    public int SummaryVersion { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastMessageAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
