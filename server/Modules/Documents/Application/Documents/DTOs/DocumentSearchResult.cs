namespace Normora.Modules.Documents.Application.Documents.DTOs;

/// <summary>
/// Represents a single ranked document chunk returned from vector similarity or hybrid search.
/// Projected into citation metadata and LLM grounding context during conversational RAG queries.
/// </summary>
public sealed record DocumentSearchResult(
    // Unique identifier of the parent Document entity.
    Guid DocumentId,

    // File name of the source document (e.g. "Employee-Benefits-2026.pdf").
    string FileName,

    // Index order of this chunk within the parent document version.
    int ChunkIndex,

    // Raw textual content of the chunk passed into the prompt context.
    string Content,

    // Cosine similarity score (0.0 to 1.0) indicating semantic relevance to the inquiry.
    double Similarity
);

