using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Domain;

public class MessageCitation : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public Guid TenantId { get; set; }
    public Guid DocumentId { get; set; }
    public Guid DocumentChunkId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int Rank { get; set; }
    public double Score { get; set; }
    public RetrievalMethod RetrievalMethod { get; set; }
    public double? RerankScore { get; set; }

    public Message Message { get; set; } = null!;
}

public enum RetrievalMethod
{
    Vector = 0,
    Keyword = 1,
    Hybrid = 2
}
