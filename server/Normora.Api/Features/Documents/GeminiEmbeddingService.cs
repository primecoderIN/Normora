using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Normora.Shared.Constants;
using Normora.Shared.Options;

namespace Normora.Api.Features.Documents;

/// <summary>
/// Implementation of <see cref="ITextEmbeddingService"/> that uses Google's Gemini API for generating 768-dimensional embeddings.
/// </summary>
public sealed class GeminiEmbeddingService(HttpClient httpClient, IOptions<GeminiOptions> options) : ITextEmbeddingService
{
    private const int EmbeddingDimensions = 768;
    private readonly string? apiKey = options.Value.ApiKey;
    private readonly string model = options.Value.EmbeddingModel;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(apiKey);

    public async Task<float[]> CreateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException(ApiMessages.GeminiEmbeddingNotConfigured);
        }

        // Convert the input text into a high-dimensional vector array so we can perform semantic similarity searches against it later
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
            throw new InvalidOperationException(ApiMessages.GeminiInvalidEmbeddingVector);
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