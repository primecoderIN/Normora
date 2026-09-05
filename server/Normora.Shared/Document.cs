using System;

namespace Normora.Shared;

/// <summary>
/// Represents an uploaded document within the system.
/// This entity is inherently bound to a specific tenant (TenantId), guaranteeing data isolation.
/// </summary>
public class Document
{
    /// <summary>
    /// The unique identifier for the document record.
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// The original file name uploaded by the user (e.g., "Q3_Report.pdf").
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// The unique object key used to store and retrieve the physical file from the MinIO S3 bucket.
    /// </summary>
    public string MinioObjectName { get; set; } = string.Empty;

    /// <summary>
    /// Text extracted from the original file by the ingestion pipeline.
    /// </summary>
    public string? ExtractedText { get; set; }

    /// <summary>
    /// The current processing status of the document.
    /// </summary>
    public DocumentStatus Status { get; set; }

    /// <summary>
    /// The UTC timestamp when the document was uploaded.
    /// </summary>
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The identifier of the tenant that owns this document.
    /// Used by the Entity Framework Global Query Filter in DocumentsDbContext to ensure cross-tenant data isolation.
    /// </summary>
    public Guid TenantId { get; set; }
}
