namespace Normora.Modules.Documents.Application.Chunks.DTOs;

/// <summary>
/// Preview representation of a document chunk.
/// </summary>
public record DocumentChunkPreviewDto(string Text, string DocumentName, string? Section, int? PageNumber);

