using Normora.Shared.Interfaces;

namespace Normora.Modules.Documents.Infrastructure.Tools;

/// <summary>
/// Autonomous agent tool that guides workspace users and administrators to the document ingestion portal.
/// </summary>
public sealed class DocumentsUploadGuideTool : IAgentTool
{
    public string Name => "documents_upload_guide";

    public string Description =>
        "Provides requirements, supported file specifications, and interactive navigation for uploading new company documents into Normora.";

    public ToolCategory Category => ToolCategory.ReadOnly;

    public bool RequiresApproval => false;

    public ToolChipDescriptor ChipDescriptor => new(
        StartLabel: "Opening Document Ingestion Guide...",
        CompletedLabel: "Document upload instructions prepared",
        FailedLabel: "Failed to load upload guide");

    public object? ParameterSchema => null;

    public Task<AgentToolResult> ExecuteAsync(
        string argumentsJson,
        ToolExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var data = new
        {
            title = "Upload Documents to Knowledge Base",
            route = "/employer/documents",
            supportedFormats = "PDF, DOCX, TXT, PNG, JPG",
            maxFileSize = "50 MB",
            automatedPipeline = "Text extraction (OCR), chunking, pgvector embeddings, and departmental ACLs."
        };

        var output = "Documents can be uploaded via the Document Management portal at /employer/documents. Normora supports PDF, DOCX, TXT, and image files up to 50MB. Uploaded documents can be scoped to specific departments and are automatically OCR-extracted, chunked, and embedded for search and AI reasoning.";

        return Task.FromResult(new AgentToolResult(true, output, StructuredData: data));
    }
}
