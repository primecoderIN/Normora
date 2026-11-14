using System.Text.Json;
using MediatR;
using Normora.Modules.Documents.Application.Documents.Queries;
using Normora.Shared.Constants;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Documents.Infrastructure.Tools;

/// <summary>
/// Autonomous agent tool that executes dense vector similarity search across indexed document knowledge chunks.
/// </summary>
public sealed class DocumentsSearchTool(IMediator mediator) : IAgentTool
{
    public string Name => "documents_search";

    public string Description =>
        "Performs dense vector similarity search across indexed document knowledge chunks to find relevant answers and policy clauses.";

    public ToolCategory Category => ToolCategory.ReadOnly;

    public bool RequiresApproval => false;

    public ToolChipDescriptor ChipDescriptor => new(
        StartLabel: "Searching indexed document chunks...",
        CompletedLabel: "Found relevant document sections",
        FailedLabel: "Failed to search document chunks");

    public object ParameterSchema => new
    {
        type = "object",
        properties = new
        {
            query = new
            {
                type = "string",
                description = "The specific query, topic, or question to search within document text."
            },
            limit = new
            {
                type = "integer",
                description = "Maximum number of nearest neighbor chunks to return (1 to 5, defaults to 3)."
            }
        },
        required = new[] { "query" }
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

            var query = root.TryGetProperty("query", out var q) ? q.GetString() : null;
            if (string.IsNullOrWhiteSpace(query))
            {
                return new AgentToolResult(false, string.Empty, ApiMessages.ToolQueryRequired);
            }

            var limit = root.TryGetProperty("limit", out var l) && l.TryGetInt32(out var parsedLimit)
                ? Math.Clamp(parsedLimit, 1, 5)
                : 3;

            var searchResults = await mediator.Send(new SearchDocumentsQuery(query, limit), cancellationToken);

            var excerpts = searchResults.Select(r => new
            {
                documentId = r.DocumentId,
                fileName = r.FileName,
                chunkIndex = r.ChunkIndex,
                content = r.Content,
                similarity = Math.Round(r.Similarity, 3)
            }).ToList();

            var result = new
            {
                query,
                totalFound = excerpts.Count,
                results = excerpts
            };

            var outputJson = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
            return new AgentToolResult(true, outputJson, StructuredData: result);
        }
        catch (Exception ex)
        {
            return new AgentToolResult(false, string.Empty, ex.Message ?? ApiMessages.DocumentSearchFailed);
        }
    }
}
