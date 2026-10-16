using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Normora.Shared.Options;
using Normora.Modules.Conversations.Application.Services;

namespace Normora.Modules.Conversations.Infrastructure.Llm;

public class GeminiQueryRewriterService : IQueryRewriterService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GeminiQueryRewriterService> _logger;
    private readonly string? apiKey;
    private readonly string model;

    public GeminiQueryRewriterService(
        HttpClient httpClient,
        IOptions<GeminiOptions> options,
        ILogger<GeminiQueryRewriterService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        var geminiOptions = options.Value;
        apiKey = geminiOptions.ApiKey;
        model = geminiOptions.GenerationModel;

        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("Gemini API key is not configured. Query rewriter service will fail.");
        }
    }

    public async Task<string> RewriteQueryAsync(
        string currentQuestion,
        IReadOnlyList<ConversationMessageContext> conversationHistory,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Gemini generation is not configured.");
        }

        // Skip rewriting if this is the very first question in the conversation since there's no context yet
        if (conversationHistory.Count == 0)
        {
            return currentQuestion; // No history to rewrite from
        }

        var historyText = string.Join("\n", conversationHistory.Select(m => $"{m.Role}: {m.Content}"));

        // Ask the LLM to rewrite the user's latest query into a standalone question using the chat history so we can search the vector DB effectively
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

        using var response = await _httpClient.PostAsJsonAsync(
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
