using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Domain;

/// <summary>
/// Source document citation attributing a generated assistant answer to a specific ingested document chunk.
/// Captures retrieval scoring, ranking, and the matching mechanism (vector, keyword, or hybrid).
/// </summary>
public class MessageCitation : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }

    /// <summary>Tenant identifier ensuring cross-tenant citation isolation.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Unique identifier of the parent document in MinIO/PostgreSQL.</summary>
    public Guid DocumentId { get; set; }

    /// <summary>Unique identifier of the specific chunk matched in the vector store.</summary>
    public Guid DocumentChunkId { get; set; }

    /// <summary>Original file name displayed in the UI citation pills.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Rank position in the retrieval result set (1 = highest confidence).</summary>
    public int Rank { get; set; }

    /// <summary>Raw similarity or relevance score from the retrieval stage.</summary>
    public double Score { get; set; }

    /// <summary>Retrieval strategy used to discover this passage.</summary>
    public RetrievalMethod RetrievalMethod { get; set; }

    /// <summary>Cross-encoder reranking score if reranking was performed.</summary>
    public double? RerankScore { get; set; }

    public Message Message { get; set; } = null!;
}

/// <summary>
/// Method used to retrieve context passages from the knowledge base.
/// </summary>
public enum RetrievalMethod
{
    Vector = 0,
    Keyword = 1,
    Hybrid = 2
}

