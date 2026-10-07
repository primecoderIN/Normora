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

    // Domain Specific
    public const string UserGroupExists = "A User Group with this name already exists.";
    public const string DepartmentExists = "A department with this name already exists.";
    public const string InvitationInvalidOrMissing = "This invitation is invalid or does not exist.";
    public const string InvitationExpired = "This invitation link has expired.";
    public const string InvitationEmailMismatch = "This invitation was sent to a different email address.";
    public const string InvitationAlreadyProcessed = "This invitation has already been processed.";
    public const string InvitationAlreadyAccepted = "This invitation has already been accepted or is no longer valid.";
    public const string PendingInvitationExists = "A pending invitation already exists for this email.";
    
    // Feature Specific
    public const string FeedbackOnlyForAssistant = "Feedback can only be submitted for assistant messages.";
    public const string SaveOnlyAssistant = "Only assistant messages can be saved.";
    public const string EmbeddingsNotConfigured = "Document search requires AI embeddings to be configured.";
    public const string DocumentNotFound = "The specified document was not found in this workspace.";
}
