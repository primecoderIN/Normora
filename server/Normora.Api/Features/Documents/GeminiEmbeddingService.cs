using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Normora.Api.Features.Documents;

public sealed class GeminiEmbeddingService(
    HttpClient httpClient,
    IConfiguration configuration) : ITextEmbeddingService
{
    private const int EmbeddingDimensions = 768;
    private readonly string? apiKey = configuration["Gemini:ApiKey"];
    private readonly string model = configuration["Gemini:EmbeddingModel"] ?? "gemini-embedding-001";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(apiKey);

    public async Task<float[]> CreateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("Gemini embedding is not configured.");
        }

        var request = new GeminiEmbeddingRequest(
            new GeminiContent([new GeminiPart(text)]),
            EmbeddingDimensions);

        using var response = await httpClient.PostAsJsonAsync(
            $"models/{model}:embedContent?key={apiKey}",
            request,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<GeminiEmbeddingResponse>(cancellationToken);
        var values = result?.Embedding?.Values;
        if (values is null || values.Count != EmbeddingDimensions)
        {
            throw new InvalidOperationException("Gemini returned an invalid embedding vector.");
        }

        return values.ToArray();
    }

    private sealed record GeminiEmbeddingRequest(
        GeminiContent Content,
        [property: JsonPropertyName("outputDimensionality")] int OutputDimensionality);

    private sealed record GeminiContent(List<GeminiPart> Parts);
    private sealed record GeminiPart(string Text);
    private sealed record GeminiEmbeddingResponse(GeminiEmbedding? Embedding);
    private sealed record GeminiEmbedding([property: JsonPropertyName("values")] List<float>? Values);
}