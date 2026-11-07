namespace Normora.Modules.Documents.Application.Services;

/// <summary>
/// Domain service contract defining vector embedding generation from textual passages.
/// Implemented by <see cref="GeminiEmbeddingService"/> using Google Gemini's text-embedding-004 model.
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
}

