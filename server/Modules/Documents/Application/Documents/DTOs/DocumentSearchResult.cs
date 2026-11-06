namespace Normora.Modules.Documents.Application.Documents.DTOs;

/// <summary>
/// Represents a single document chunk returned from a semantic search.
/// </summary>
public sealed record DocumentSearchResult(
    Guid DocumentId,
    string FileName,
    int ChunkIndex,
    string Content,
    double Similarity);

