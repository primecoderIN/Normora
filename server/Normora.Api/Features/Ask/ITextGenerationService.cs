namespace Normora.Api.Features.Ask;

/// <summary>
/// Defines the contract for generating natural language answers based on retrieved document chunks.
/// </summary>
public interface ITextGenerationService
{
    /// <summary>
    /// Gets a value indicating whether the generation service is properly configured (e.g., API keys exist).
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Generates a grounded answer to a user's question, exclusively using the provided sources.
    /// </summary>
    /// <param name="question">The employee's question.</param>
    /// <param name="sources">The retrieved chunks of text to use as evidence.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A generated answer, or a default string if no answer can be found in the sources.</returns>
    Task<string> GenerateGroundedAnswerAsync(
        string question,
        IReadOnlyList<AskSource> sources,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a specific chunk of text from a document used as evidence for text generation.
/// </summary>
/// <param name="FileName">The name of the source document.</param>
/// <param name="ChunkIndex">The zero-based index of this chunk within the document.</param>
/// <param name="Content">The raw extracted text of the chunk.</param>
public sealed record AskSource(string FileName, int ChunkIndex, string Content);