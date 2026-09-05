namespace Normora.Modules.Tenants.Domain;

/// <summary>
/// Junction entity assigning a UserGroup to one or more Departments.
/// When a user is a member of a group, they inherit access to all departments
/// linked to that group. This is the key mechanism for group-based department inheritance.
/// </summary>
public class UserGroupDepartment
{
    public Guid UserGroupId { get; set; }
    public Guid DepartmentId { get; set; }

    // Navigation Properties
    public UserGroup UserGroup { get; set; } = null!;
    public Department Department { get; set; } = null!;
}
