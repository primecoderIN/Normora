using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Normora.Modules.Conversations.Application.Services;

namespace Normora.Modules.Conversations.Infrastructure.Llm;

public class GeminiQueryRewriterService(
    HttpClient httpClient,
    IConfiguration configuration) : IQueryRewriterService
{
    private readonly string? apiKey = configuration["Gemini:ApiKey"];
    private readonly string model = configuration["Gemini:GenerationModel"] ?? "gemini-2.0-flash";

    public async Task<string> RewriteQueryAsync(
        string currentQuestion,
        IReadOnlyList<ConversationMessageContext> conversationHistory,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Gemini generation is not configured.");
        }

        if (conversationHistory.Count == 0)
        {
            return currentQuestion; // No history to rewrite from
        }

        var historyText = string.Join("\n", conversationHistory.Select(m => $"{m.Role}: {m.Content}"));

        var prompt = $"""
            Given the following conversation history and a follow-up user question, rephrase the follow-up question to be a standalone question that can be used to query a vector database.
            Do NOT answer the question. Only return the standalone question. If the question does not need rewriting, return it exactly as is.

            Conversation History:
            {historyText}

            Follow-up Question:
            {currentQuestion}

            Standalone Question:
            """;

        var request = new GeminiGenerateRequest(
            [new GeminiContent([new GeminiPart(prompt)])],
            new GeminiGenerationConfig(0.0)); // low temperature for consistent rewrites

        using var response = await httpClient.PostAsJsonAsync(
            $"models/{model}:generateContent?key={apiKey}",
            request,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<GeminiGenerateResponse>(cancellationToken);
        var rewritten = result?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
        
        return string.IsNullOrWhiteSpace(rewritten) ? currentQuestion : rewritten.Trim();
    }

    private sealed record GeminiGenerateRequest(
        List<GeminiContent> Contents,
        [property: JsonPropertyName("generationConfig")] GeminiGenerationConfig GenerationConfig);

    private sealed record GeminiContent(List<GeminiPart> Parts);
    private sealed record GeminiPart(string Text);
    private sealed record GeminiGenerationConfig([property: JsonPropertyName("temperature")] double Temperature);
    private sealed record GeminiGenerateResponse(List<GeminiCandidate>? Candidates);
    private sealed record GeminiCandidate(GeminiContent? Content);
}
