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
    /// Content-Type of the file (e.g., application/pdf)
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Size of the document in bytes.
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// The UTC timestamp when the document was uploaded.
    /// </summary>
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The identifier of the tenant that owns this document.
    /// Used by the Entity Framework Global Query Filter in DocumentsDbContext to ensure cross-tenant data isolation.
    /// </summary>
    public Guid TenantId { get; set; }

    // Navigation Properties

    /// <summary>
    /// The departments this document is scoped to. An empty collection means the document
    /// is "Company Wide" and accessible to every employee in the tenant.
    /// </summary>
    public ICollection<DocumentDepartment> DocumentDepartments { get; set; } = new List<DocumentDepartment>();

    /// <summary>
    /// The versions of this document.
    /// </summary>
    public ICollection<DocumentVersion> Versions { get; set; } = new List<DocumentVersion>();
}
