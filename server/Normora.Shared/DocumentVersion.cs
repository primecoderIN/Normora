using System;
using System.Collections.Generic;

namespace Normora.Shared;

/// <summary>
/// Represents a specific version of a document.
/// </summary>
public class DocumentVersion
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Guid TenantId { get; set; }

    /// <summary>
    /// Sequential version number (1, 2, 3...)
    /// </summary>
    public int VersionNumber { get; set; }

    /// <summary>
    /// The unique object key used to store and retrieve the physical file from the MinIO S3 bucket.
    /// </summary>
    public string MinioObjectName { get; set; } = string.Empty;

    /// <summary>
    /// Text extracted from the original file by the ingestion pipeline.
    /// </summary>
    public string? ExtractedText { get; set; }

    /// <summary>
    /// The current processing status of this version.
    /// </summary>
    public DocumentStatus Status { get; set; }

    /// <summary>
    /// Only the active version is queried by RAG.
    /// </summary>
    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Document? Document { get; set; }
    public ICollection<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();
}
