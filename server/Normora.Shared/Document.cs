using System;

namespace Normora.Shared;

/// <summary>
/// Represents an uploaded document within the system.
/// This entity is inherently bound to a specific tenant (TenantId), guaranteeing data isolation.
/// </summary>
public class Document
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// The original file name uploaded by the user (e.g., "Q3_Report.pdf").
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// The unique object key used to store and retrieve the physical file from the MinIO S3 bucket.
    /// </summary>
    public string MinioObjectName { get; set; } = string.Empty;

    public DocumentStatus Status { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The identifier of the tenant that owns this document.
    /// This is automatically populated by AppDbContext.SaveChangesAsync() and filtered by Global Query Filters.
    /// </summary>
    public Guid TenantId { get; set; }
}
