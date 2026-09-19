namespace Normora.Shared.Constants;

/// <summary>
/// Defines the standard roles available within a tenant.
/// Use these constants instead of magic strings for authorization checks.
/// </summary>
public static class TenantRoles
{
    public const string Admin = "admin";
    public const string Employee = "employee";
}
