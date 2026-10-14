namespace Normora.Shared.Interfaces;

/// <summary>
/// Marks a MediatR request as auditable. When handled successfully, the AuditBehavior
/// intercepts the execution and persists a record in the tenant's audit trail.
/// </summary>
public interface IAuditableRequest
{
    /// <summary>
    /// The dot-notated action name, e.g. "Document.Uploaded", "Employee.Removed", "Department.Created".
    /// </summary>
    string Action { get; }

    /// <summary>
    /// The logical category or type of entity affected, e.g. "Document", "Employee", "Department", "UserGroup", "TenantBranding".
    /// </summary>
    string TargetType { get; }

    /// <summary>
    /// Extracts the identifier of the target entity, optionally inspecting the handler's response.
    /// </summary>
    string? GetTargetId(object? response);

    /// <summary>
    /// Extracts structured JSON metadata or details regarding the mutation.
    /// </summary>
    string? GetMetadata(object? response);
}
