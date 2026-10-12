using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Normora.Modules.Conversations.Application.Services;
using Normora.Shared.Options;

namespace Normora.Modules.Conversations.Infrastructure.Llm;

/// <summary>
/// Calls Gemini to generate a short list of questions an employee might ask
/// based on the document file names available in their effective departments.
/// This is a lightweight generation call — no embedding or retrieval involved.
/// </summary>
public class GeminiSuggestionsService(
    HttpClient httpClient,
    IOptions<GeminiOptions> options,
    ILogger<GeminiSuggestionsService> logger) : ISuggestionsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<IReadOnlyList<string>> GenerateSuggestionsAsync(
        IReadOnlyList<string> documentNames,
        CancellationToken cancellationToken = default)
    {
        var apiKey = options.Value.ApiKey;
        var model = options.Value.GenerationModel;

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            logger.LogWarning("Gemini API key is not configured. Suggestions will be empty.");
            return [];
        }

        if (documentNames.Count == 0)
        {
            return [];
        }

        var documentList = string.Join("\n", documentNames.Select(n => $"- {n}"));

        // Low-temperature prompt: we want consistent, sensible suggestions, not creative ones.
        var prompt = $"""
            You are an assistant helping employees explore their company's document knowledge base.
            Based on the following list of company document names, generate exactly 5 short, natural-language questions that an employee might realistically ask.
            Each question should be directly answerable from the kind of content those documents would contain.
            
            Rules:
            - Return ONLY a valid JSON array of 5 strings. No markdown, no explanation, no wrapper.
            - Each string is a complete question (15–60 characters).
            - Do NOT number the questions.
            - Questions should be varied and cover different document topics.
            
            Company documents:
            {documentList}
            
            JSON array:
            """;

        var requestBody = new GeminiGenerateRequest(
            [new GeminiContent([new GeminiPart(prompt)])],
            new GeminiGenerationConfig(0.2));

        try
        {
            using var response = await httpClient.PostAsJsonAsync(
                $"models/{model}:generateContent?key={apiKey}",
                requestBody,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<GeminiGenerateResponse>(
                JsonOptions, cancellationToken);

            var raw = result?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

            if (string.IsNullOrWhiteSpace(raw))
            {
                logger.LogWarning("Gemini returned an empty suggestions response.");
                return [];
            }

            // Strip markdown code fences if the model wraps the JSON anyway
            raw = raw.Trim();
            if (raw.StartsWith("```"))
            {
                raw = raw.Split('\n').Skip(1).TakeWhile(l => !l.StartsWith("```")).Aggregate((a, b) => $"{a}\n{b}");
            }

            var parsed = JsonSerializer.Deserialize<List<string>>(raw, JsonOptions);
            if (parsed is null || parsed.Count == 0)
            {
                logger.LogWarning("Gemini suggestions response could not be parsed as a string array.");
                return [];
            }

            return parsed
                .Where(q => !string.IsNullOrWhiteSpace(q))
                .Take(5)
                .ToList()
                .AsReadOnly();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate suggestions from Gemini.");
            return [];
        }
    }

    // ── Private record types for Gemini JSON serialization ──────────────────

    private sealed record GeminiGenerateRequest(
        List<GeminiContent> Contents,
        [property: JsonPropertyName("generationConfig")] GeminiGenerationConfig GenerationConfig);

    private sealed record GeminiContent(List<GeminiPart> Parts);
    private sealed record GeminiPart(string Text);
    private sealed record GeminiGenerationConfig([property: JsonPropertyName("temperature")] double Temperature);
    private sealed record GeminiGenerateResponse(List<GeminiCandidate>? Candidates);
    private sealed record GeminiCandidate(GeminiContent? Content);
}
