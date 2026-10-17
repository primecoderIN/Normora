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
    public const string AskGeminiNotConfigured = "Ask Normora requires Gemini generation to be configured.";
    public const string McpInvalidRequest = "Invalid MCP JSON-RPC request.";
    public const string McpMethodNotFound = "The requested MCP method was not found.";
    public const string McpToolNotFound = "The requested MCP tool was not found.";
    public const string McpResourceNotFound = "The requested MCP resource was not found.";
    
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

    // Subscription & Plan Limits
    public const string PlanDocumentLimitExceeded = "You have reached your workspace document limit. Please upgrade your subscription to upload more documents.";
    public const string PlanEmployeeLimitExceeded = "You have reached your workspace member limit. Please upgrade your subscription to invite more members.";
    public const string PlanStorageLimitExceeded = "This file exceeds your workspace remaining storage quota. Please upgrade your subscription.";
    public const string PlanLimitExceeded = "Your workspace has exceeded its subscription plan limits.";

    // Slug & Tenancy Errors
    public const string TenantSlugTaken = "This workspace URL slug is already taken.";
    public const string TenantContextAlreadySet = "Tenant context is already set for this request.";
    public const string HubTenantAccessDenied = "You do not have access to this tenant.";
    public const string GeminiEmbeddingNotConfigured = "Gemini embedding is not configured.";
    public const string GeminiInvalidEmbeddingVector = "Gemini returned an invalid embedding vector.";

    // Success Messages
    public const string TenantCreated = "Tenant created successfully.";
    public const string TenantSuspended = "Tenant suspended successfully.";
    public const string InvitationSent = "Invitation sent successfully.";
    public const string InvitationAccepted = "Invitation accepted successfully.";
    public const string InvitationRejected = "Invitation rejected.";
    public const string EmployeeRemoved = "Employee removed successfully.";
    public const string DocumentUploaded = "Document uploaded successfully.";
    public const string DocumentDeleted = "Document deleted successfully.";
    public const string DepartmentCreated = "Department created successfully.";
    public const string DepartmentUpdated = "Department updated successfully.";
    public const string DepartmentDeleted = "Department deleted successfully.";
    public const string UserGroupCreated = "User group created successfully.";
    public const string UserGroupUpdated = "User group updated successfully.";
    public const string UserGroupAssignmentsUpdated = "User group assignments updated successfully.";
    public const string UserGroupDeleted = "User group deleted successfully.";
    public const string UserAssignmentsUpdated = "User assignments updated successfully.";
    public const string SubscriptionUpdated = "Subscription plan updated successfully.";
    public const string CheckoutSessionCreated = "Checkout session created successfully.";
    public const string CustomerPortalCreated = "Billing portal session created successfully.";
    public const string FreeTierCannotCheckout = "Free tier does not require payment processing.";
    public const string StripeBillingAccountNotFound = "No Stripe billing account found for this workspace. Please subscribe to a plan first.";
    public const string AnswerSaved = "Answer saved successfully.";
}


