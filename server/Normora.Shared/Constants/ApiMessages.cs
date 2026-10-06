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
    public const string Forbidden = "You do not have permission to perform this action.";

    // Validation & Bad Requests
    public const string ValidationFailed = "One or more validation errors occurred.";
    public const string BadRequest = "The request could not be processed.";
    public const string InvalidSlug = "Invalid tenant slug.";
    public const string InvalidInvitation = "Failed to accept invitation. It may be expired or invalid.";
    public const string SearchQueryRequired = "A search query is required.";

    // Resource
    public const string NotFound = "The requested resource was not found.";

    // Server Errors
    public const string InternalServerError = "An unexpected error occurred. Please try again later.";
    
    // Actions
    public const string CannotRemoveLastAdmin = "Cannot remove the last administrator from the workspace.";
}
