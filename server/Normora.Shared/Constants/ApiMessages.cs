namespace Normora.Shared.Constants;

/// <summary>
/// Centralized API response messages to avoid magic strings and ensure consistency across the application.
/// </summary>
public static class ApiMessages
{
    // Authorization & Tenancy
    public const string TenantContextMissing = "Tenant context is missing or invalid.";
    public const string RoleForbid = "You do not have the required role in this workspace.";
    public const string Unauthorized = "User is not authenticated or authorized.";

    // Validation & Bad Requests
    public const string ValidationFailed = "One or more validation errors occurred.";
    public const string BadRequest = "The request could not be processed.";

    // Server Errors
    public const string InternalServerError = "An unexpected error occurred. Please try again later.";
}
