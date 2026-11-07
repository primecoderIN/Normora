namespace Normora.Modules.Documents.Application.Chunks.DTOs;

/// <summary>
/// Preview representation of an extracted document text chunk returned for user citation drawers.
/// Renders the verified excerpt alongside page and section metadata in the employee chat interface.
/// </summary>
public record DocumentChunkPreviewDto(
    // The raw textual content of the chunk retrieved from pgvector storage.
    string Text,

    // The original file name of the source document (e.g. "Leave-Policy-2026.pdf").
    string DocumentName,

    // Optional heading or section title identified during semantic document decomposition.
    string? Section,

    // 1-based page number within the physical PDF/DOCX file where this text resides.
    int? PageNumber
);

