using System.Text.Json;
using MediatR;
using Normora.Modules.Documents.Application.Documents.Commands;
using Normora.Shared.Constants;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Documents.Infrastructure.Tools;

/// <summary>
/// Autonomous agent tool that marks a knowledge base document as soft-deleted, initiating a 30-day retention countdown.
/// Destructive operation requiring explicit human-in-the-loop confirmation.
/// </summary>
public sealed class DocumentsDeleteTool(IMediator mediator) : IAgentTool
{
    public string Name => "documents_delete";

    public string Description =>
        "Soft-deletes a company knowledge base document by its unique GUID identifier, immediately excluding it from search and initiating a 30-day purge grace period. Requires confirmed=true.";

    public ToolCategory Category => ToolCategory.Destructive;

    public bool RequiresApproval => true;

    public ToolChipDescriptor ChipDescriptor => new(
        StartLabel: "Deleting company knowledge document...",
        CompletedLabel: "Document successfully soft-deleted",
        FailedLabel: "Failed to delete document");

    public object ParameterSchema => new
    {
        type = "object",
        properties = new
        {
            documentId = new
            {
                type = "string",
                description = "The unique GUID identifier of the document to delete."
            },
            confirmed = new
            {
                type = "boolean",
                description = "Must be explicitly set to true to confirm deletion."
            }
        },
        required = new[] { "documentId", "confirmed" }
    };

    public async Task<AgentToolResult> ExecuteAsync(
        string argumentsJson,
        ToolExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(argumentsJson) ? "{}" : argumentsJson);
            var root = doc.RootElement;

            if (!root.TryGetProperty("documentId", out var idProp) || !Guid.TryParse(idProp.GetString(), out var documentId))
            {
                return new AgentToolResult(false, string.Empty, ApiMessages.ValidationFailed);
            }

            var confirmed = root.TryGetProperty("confirmed", out var confProp) && confProp.GetBoolean();
            if (!confirmed)
            {
                return new AgentToolResult(
                    false,
                    string.Empty,
                    "Deletion halted: 'confirmed' parameter must be explicitly true to delete documents.");
            }

            var userId = Guid.TryParse(context.UserId, out var parsedUser) ? parsedUser : Guid.Empty;

            var deleted = await mediator.Send(
                new DeleteDocumentCommand(documentId, context.TenantId, userId),
                cancellationToken);

            if (!deleted)
            {
                return new AgentToolResult(false, string.Empty, ApiMessages.NotFound);
            }

            var result = new
            {
                status = "deleted",
                documentId,
                gracePeriodDays = 30,
                message = $"Document '{documentId}' has been soft-deleted from {context.TenantName}."
            };

            var outputJson = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
            return new AgentToolResult(true, outputJson, StructuredData: result, ChangedData: true);
        }
        catch (Exception ex)
        {
            return new AgentToolResult(false, string.Empty, ex.Message ?? ApiMessages.InternalServerError);
        }
    }
}

