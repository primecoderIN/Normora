using System.Text.Json;

namespace Normora.Api.Features.Ask.Tools;

/// <summary>
/// Tool that performs a targeted semantic and keyword search over company knowledge documents.
/// </summary>
public sealed class SearchCompanyDocumentsTool(IRetrievalService retrievalService) : IAgentTool
{
    public string Name => "search_company_documents";

    public string Description =>
        "Performs a targeted semantic and keyword search across the workspace's company policy documents to discover relevant sections and excerpts.";

    public object ParameterSchema => new
    {
        type = "object",
        properties = new
        {
            query = new
            {
                type = "string",
                description = "The specific search term, phrase, or policy topic to query in company documentation."
            },
            limit = new
            {
                type = "integer",
                description = "Maximum number of document excerpts to return (1 to 5). Defaults to 3."
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
                return new AgentToolResult(false, string.Empty, "The 'query' parameter is required.");
            }

            var limit = root.TryGetProperty("limit", out var l) && l.TryGetInt32(out var parsedLimit)
                ? Math.Clamp(parsedLimit, 1, 5)
                : 3;

            var (candidates, _) = await retrievalService.RunHybridRetrievalAsync(
                query,
                limit,
                context.PersonalTenantId,
                cancellationToken);

            var excerpts = candidates.Select(c => new
            {
                documentId = c.DocumentId,
                fileName = c.FileName,
                chunkIndex = c.ChunkIndex,
                relevanceScore = Math.Round(c.Score, 3),
                content = c.Content
            }).ToList();

            var result = new
            {
                query,
                totalFound = excerpts.Count,
                results = excerpts
            };

            var outputJson = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
            return new AgentToolResult(true, outputJson);
        }
        catch (Exception ex)
        {
            return new AgentToolResult(false, string.Empty, $"Document search failed: {ex.Message}");
        }
    }
}
