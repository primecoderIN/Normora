using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Normora.Shared.Constants;
using Normora.Shared.Interfaces;
using Normora.Shared.Options;

namespace Normora.Modules.Documents.Application.Services;

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
        // Provider Configuration Guard:
        // Ensures Gemini API Key is configured before issuing HTTP calls to the Google Generative AI endpoints.
        if (!IsConfigured)
        {
            throw new InvalidOperationException(ApiMessages.EmbeddingServiceNotConfigured);
        }

        // Semantic Vector Generation (Google Gemini text-embedding-004):
        // Requests a 768-dimensional embedding from the Gemini API using the outputDimensionality parameter.
        // The 768-dimensional space matches the PostgreSQL pgvector HNSW index definition in DocumentChunkConfiguration.
        var request = new GeminiEmbeddingRequest(
            new GeminiContent([new GeminiPart(text)]),
            EmbeddingDimensions);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"models/{model}:embedContent")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Add("x-goog-api-key", apiKey);

        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<GeminiEmbeddingResponse>(cancellationToken);
        var values = result?.Embedding?.Values;

        // Vector Dimension Invariant Verification:
        // Strictly verify that the API returned exactly 768 float values.
        // Any dimensional mismatch would cause PostgreSQL pgvector insert failures.
        if (values is null || values.Count != EmbeddingDimensions)
        {
            throw new InvalidOperationException(ApiMessages.InvalidEmbeddingVector);
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
