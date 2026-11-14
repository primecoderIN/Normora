using System.Text.Json;
using MediatR;
using Normora.Modules.Documents.Application.Documents.Queries;
using Normora.Shared.Constants;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Documents.Infrastructure.Tools;

/// <summary>
/// Autonomous agent tool that lists all active knowledge base documents in the organization.
/// </summary>
public sealed class DocumentsListTool(IMediator mediator) : IAgentTool
{
    public string Name => "documents_list";

    public string Description =>
        "Lists all active company documents in the knowledge base with their upload dates, processing status, and department access scopes.";

    public ToolCategory Category => ToolCategory.ReadOnly;

    public bool RequiresApproval => false;

    public ToolChipDescriptor ChipDescriptor => new(
        StartLabel: "Listing workspace knowledge documents...",
        CompletedLabel: "Document catalog retrieved",
        FailedLabel: "Failed to list documents");

    public object ParameterSchema => new
    {
        type = "object",
        properties = new { },
        required = Array.Empty<string>()
    };

    public async Task<AgentToolResult> ExecuteAsync(
        string argumentsJson,
        ToolExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var documents = await mediator.Send(new GetEmployerDocumentsQuery(), cancellationToken);

            var result = new
            {
                totalDocuments = documents.Count,
                documents = documents.Select(d => new
                {
                    id = d.Id,
                    fileName = d.FileName,
                    status = d.Status,
                    uploadedAt = d.UploadedAt,
                    departmentScopesCount = d.DepartmentIds.Count,
                    versionNumber = d.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault()?.VersionNumber ?? 1
                }).ToList()
            };

            var outputJson = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
            return new AgentToolResult(true, outputJson, StructuredData: result);
        }
        catch (Exception ex)
        {
            return new AgentToolResult(false, string.Empty, ex.Message ?? ApiMessages.InternalServerError);
        }
    }
}

