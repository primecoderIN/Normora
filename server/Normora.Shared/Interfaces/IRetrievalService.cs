namespace Normora.Shared.Interfaces;

/// <summary>Exposes tenant-safe hybrid retrieval without leaking persistence contexts.</summary>
public record RetrievalCandidate
{
    public Guid ChunkId { get; set; }
    public Guid DocumentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int ChunkIndex { get; set; }
    public string Content { get; set; } = string.Empty;
    public double Score { get; set; }
    public double VectorSimilarity { get; set; }
}

/// <summary>
/// Encapsulates the hybrid retrieval logic (Vector dense search + PostgreSQL full-text search + Reciprocal Rank Fusion)
/// for grounded document knowledge without exposing raw database contexts across module boundaries.
/// </summary>
public interface IRetrievalService
{
    Task<(List<RetrievalCandidate> Candidates, double VectorSimOfFirst)> RunHybridRetrievalAsync(
        string question,
        int limit,
        Guid? personalTenantId,
        CancellationToken ct = default);
}
