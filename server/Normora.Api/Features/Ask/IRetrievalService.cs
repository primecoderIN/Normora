namespace Normora.Api.Features.Ask;

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
/// Encapsulates the complex EF Core Postgres-specific hybrid search logic (Vector + FTS + RRF).
/// Extracting this from the MediatR command handlers improves SRP, DRY, and allows unit testing of the orchestration logic.
/// </summary>
public interface IRetrievalService
{
    Task<(List<RetrievalCandidate> Candidates, double VectorSimOfFirst)> RunHybridRetrievalAsync(
        string question, 
        int limit, 
        Guid? personalTenantId, 
        CancellationToken ct = default);
}
