using System;
using System.Collections.Generic;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Documents.Domain;

/// <summary>
/// Domain Entity representing a physical revision/version of a root Document container.
/// Invariants & Retrieval Architecture:
/// 1. IsActive: Only ONE version per document is marked IsActive == true at any time.
///    The conversational RAG hybrid retrieval engine filters exclusively on IsActive == true
///    to guarantee that users never receive answers derived from superseded or stale versions.
/// 2. StorageObjectName: References the immutable object key in MinIO/S3 object storage ("{tenantId}/{guid}_{fileName}").
/// 3. Chunks: Navigates to the collection of semantic text chunks and pgvector embeddings generated for this revision.
/// </summary>
public class DocumentVersion : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Guid TenantId { get; set; }

    /// <summary>
    /// Sequential monotonic version number (1, 2, 3...) within the document container.
    /// </summary>
    public int VersionNumber { get; set; }

    /// <summary>
    /// S3 object key pointing to the physical binary in MinIO object storage.
    /// </summary>
    public string StorageObjectName { get; set; } = string.Empty;

    /// <summary>
    /// Raw full-text extracted by the document text extractor during background ingestion.
    /// </summary>
    public string? ExtractedText { get; set; }

    /// <summary>
    /// Ingestion pipeline state: Uploaded -> Processing -> Ready (or Failed).
    /// </summary>
    public DocumentStatus Status { get; set; }

    /// <summary>
    /// Invariant: Only the active version is matched by full-text search and vector cosine similarity.
    /// </summary>
    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public void MarkProcessing() => Status = DocumentStatus.Processing;
    public void MarkReady() => Status = DocumentStatus.Ready;
    public void MarkFailed() => Status = DocumentStatus.Failed;

    public Document? Document { get; set; }
    public ICollection<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();
}
