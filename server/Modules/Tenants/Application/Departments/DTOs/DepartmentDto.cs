namespace Normora.Modules.Tenants.Application.Departments.DTOs;

/// <summary>
/// Data transfer object representing an organizational department.
/// Used in employer management interfaces and document access scope dropdowns.
/// </summary>
public record DepartmentDto(
    // Unique identifier of the department.
    Guid Id,

    // Display name of the department (e.g. "Human Resources", "Legal").
    string Name
);

