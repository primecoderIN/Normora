namespace Normora.Api.Features.Documents;

/// <summary>
/// Defines the contract for generating dense vector embeddings from raw text.
/// </summary>
public interface ITextEmbeddingService
{
    /// <summary>
    /// Gets a value indicating whether the embedding service is properly configured.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Creates a vector embedding for the provided text.
    /// </summary>
    /// <param name="text">The text to embed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An array of floats representing the embedding vector.</returns>
    Task<float[]> CreateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
}