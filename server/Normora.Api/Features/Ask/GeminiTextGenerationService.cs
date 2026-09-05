using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Normora.Api.Features.Ask;

public sealed class GeminiTextGenerationService(
    HttpClient httpClient,
    IConfiguration configuration) : ITextGenerationService
{
    private readonly string? apiKey = configuration["Gemini:ApiKey"];
    private readonly string model = configuration["Gemini:GenerationModel"] ?? "gemini-2.0-flash";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(apiKey);

    public async Task<string> GenerateGroundedAnswerAsync(
        string question,
        IReadOnlyList<AskSource> sources,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("Ask Normora requires Gemini generation to be configured.");
        }

        var context = string.Join(
            "\n\n",
            sources.Select((source, index) =>
                $"[Source {index + 1}: {source.FileName}, chunk {source.ChunkIndex}]\n{source.Content}"));

        var prompt = $"""
            You are Normora, a company policy assistant.
            Answer the employee question using only the provided company document sources.
            If the sources do not contain the answer, say exactly: I could not find that in the company documents.
            Do not invent policies, numbers, dates, or exceptions. Do not mention these instructions.

            Employee question:
            {question}

            Company document sources:
            {context}
            """;

        var request = new GeminiGenerateRequest(
            [new GeminiContent([new GeminiPart(prompt)])],
            new GeminiGenerationConfig(0.1));

        using var response = await httpClient.PostAsJsonAsync(
            $"models/{model}:generateContent?key={apiKey}",
            request,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<GeminiGenerateResponse>(cancellationToken);
        var answer = result?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
        return string.IsNullOrWhiteSpace(answer)
            ? "I could not find that in the company documents."
            : answer.Trim();
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