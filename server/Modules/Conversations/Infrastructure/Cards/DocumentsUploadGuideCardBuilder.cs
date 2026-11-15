using Normora.Modules.Conversations.Application.Services;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Infrastructure.Cards;

/// <summary>
/// Constructs an interactive action card with 1-click actuation navigating to the Document Management portal.
/// </summary>
public sealed class DocumentsUploadGuideCardBuilder : IActionCardBuilder
{
    public bool CanHandle(string toolName) => toolName == "documents_upload_guide";

    public ActionCardItem? BuildCard(string toolName, string argumentsJson, AgentToolResult result)
    {
        if (!result.Success) return null;

        return new ActionCardItem(
            ActionType: "document_upload_guide_card",
            Title: "Upload Documents to Knowledge Base",
            Description: "Upload company policies, handbooks, and knowledge documents. Files are automatically OCR-extracted, chunked, and indexed with departmental access control.",
            Parameters: new Dictionary<string, string>
            {
                ["Accepted Formats"] = "PDF, DOCX, TXT, PNG, JPG",
                ["Max File Size"] = "50 MB",
                ["Security Clearance"] = "Department Scoped ACLs",
                ["Destination"] = "/employer/documents",
                ["route"] = "/employer/documents"
            },
            ConfirmButtonText: "Go to Document Upload",
            CancelButtonText: "Dismiss");
    }
}

