using System;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Documents.Domain;

/// <summary>
/// Root Aggregate Entity for Document Management.
/// Acts as the permanent logical container for an uploaded organizational knowledge asset.
/// Data Isolation & Security:
/// 1. ITenantEntity enforces automatic multitenant query filtering by TenantId.
/// 2. DocumentDepartments defines RBAC/departmental visibility (empty collection = Company Wide).
/// 3. Soft-delete fields (DeletedAt, DeletedByUserId) immediately exclude this document and all
///    its underlying chunks from conversational RAG retrieval while preserving audit compliance.
/// </summary>
public class Document : ITenantEntity
{
    /// <summary>
    /// The unique primary key of the document container.
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// The original file name uploaded by the administrator (e.g. "Employee_Handbook_2026.pdf").
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Content-Type of the file (e.g. application/pdf, text/plain).
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Size of the active document payload in bytes.
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// The UTC timestamp when the root document container was created.
    /// </summary>
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The tenant workspace identifier owning this document.
    /// Bound to the ambient ITenantContext in DocumentsDbContext global query filters.
    /// </summary>
    public Guid TenantId { get; set; }

    // Navigation Properties

    /// <summary>
    /// Departmental ACL mappings:
    /// - If empty: The document is accessible company-wide across the tenant.
    /// - If populated: Access is restricted strictly to employees belonging to at least one of these departments.
    /// </summary>
    public ICollection<DocumentDepartment> DocumentDepartments { get; set; } = new List<DocumentDepartment>();

    /// <summary>
    /// Physical revision history. Only the version marked IsActive == true is matched during RAG retrieval.
    /// </summary>
    public ICollection<DocumentVersion> Versions { get; set; } = new List<DocumentVersion>();

    // --- Soft-Delete Audit Fields ---

    /// <summary>
    /// UTC timestamp of soft-deletion.
    /// NULL indicates active document. Populated timestamp excludes the document from all RAG searches.
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// The UserId of the administrator who executed the soft-deletion.
    /// </summary>
    public Guid? DeletedByUserId { get; set; }
}
