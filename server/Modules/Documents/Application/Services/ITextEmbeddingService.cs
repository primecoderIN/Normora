namespace Normora.Modules.Documents.Application.Services;

/// <summary>
/// Domain service contract defining vector embedding generation from textual passages.
/// Implemented by provider adapters registered by the host.
/// </summary>
public interface ITextEmbeddingService
{
    /// <summary>
    /// Gets a value indicating whether the embedding service has valid API keys and endpoint configuration.
    /// When false, system falls back to BM25 full-text keyword retrieval gracefully.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Generates a dense 768-dimensional vector embedding for the supplied text string.
    /// </summary>
    /// <param name="text">The chunk or user query text to vectorize.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A float array of length 768 representing the dense vector coordinates.</returns>
    Task<float[]> CreateEmbeddingAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates dense 768-dimensional vector embeddings for a batch of textual passages.
    /// Enables high-throughput ingestion without single-chunk HTTP roundtrip overhead.
    /// </summary>
    /// <param name="texts">The list of chunk texts to vectorize.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of float arrays, each representing a dense vector.</returns>
    Task<IReadOnlyList<float[]>> CreateBatchEmbeddingsAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default);
}
