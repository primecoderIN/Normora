namespace Normora.Shared;

/// <summary>
/// Junction entity that scopes a Document to a specific Department.
/// A document with no DocumentDepartment records is considered "Company Wide" and
/// is accessible to all employees within the tenant.
/// TenantId is denormalized here to support a matching global query filter alongside
/// the Document entity filter, preventing EF Core filter mismatch warnings.
/// </summary>
public class DocumentDepartment
{
    public Guid DocumentId { get; set; }

    /// <summary>
    /// The Department this document is scoped to. References the Department in the
    /// Tenants module. Stored as a plain Guid to avoid cross-module entity references.
    /// </summary>
    public Guid DepartmentId { get; set; }

    /// <summary>
    /// Denormalized from the parent Document for tenant-isolation query filter alignment.
    /// </summary>
    public Guid TenantId { get; set; }

    // Navigation Properties
    public Document Document { get; set; } = null!;
}

