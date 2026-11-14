using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Normora.Modules.Documents.Application.Services;
using Normora.Shared.Constants;

namespace Normora.Infrastructure.Providers.Gemini;

/// <summary>
/// Implementation of <see cref="ITextEmbeddingService"/> and Microsoft.Extensions.AI <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/>
/// using Google's Gemini API (text-embedding-004) for generating 768-dimensional embeddings with native batching support.
/// </summary>
public sealed class GeminiEmbeddingService(HttpClient httpClient, IOptions<GeminiOptions> options) :
    ITextEmbeddingService,
    IEmbeddingGenerator<string, Embedding<float>>
{
    private const int EmbeddingDimensions = 768;
    private const int MaxGeminiBatchSize = 100;
    private readonly string? apiKey = options.Value.ApiKey;
    private readonly string model = options.Value.EmbeddingModel;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(apiKey);

    public EmbeddingGeneratorMetadata Metadata => new(
        providerName: "Gemini",
        providerUri: new Uri("https://generativelanguage.googleapis.com"),
        defaultModelId: model);

    // ─── ITextEmbeddingService Implementation ──────────────────────────────────

    public async Task<float[]> CreateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

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

        if (values is null || values.Count != EmbeddingDimensions)
        {
            throw new InvalidOperationException(ApiMessages.InvalidEmbeddingVector);
        }

        return values.ToArray();
    }

    public async Task<IReadOnlyList<float[]>> CreateBatchEmbeddingsAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default)
    {
        if (texts.Count == 0) return [];
        if (texts.Count == 1) return [await CreateEmbeddingAsync(texts[0], cancellationToken)];

        EnsureConfigured();

        var allResults = new List<float[]>(texts.Count);

        // Process in batches capped by Gemini's batchEmbedContents limit (100)
        foreach (var batch in texts.Chunk(MaxGeminiBatchSize))
        {
            var batchRequests = batch.Select(text => new GeminiBatchEntry(
                $"models/{model}",
                new GeminiContent([new GeminiPart(text)]),
                EmbeddingDimensions)).ToList();

            var requestPayload = new GeminiBatchEmbeddingRequest(batchRequests);

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"models/{model}:batchEmbedContents")
            {
                Content = JsonContent.Create(requestPayload)
            };
            httpRequest.Headers.Add("x-goog-api-key", apiKey);

            using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<GeminiBatchEmbeddingResponse>(cancellationToken);
            var embeddings = result?.Embeddings;

            if (embeddings is null || embeddings.Count != batch.Length)
            {
                throw new InvalidOperationException(ApiMessages.InvalidEmbeddingVector);
            }

            foreach (var emb in embeddings)
            {
                if (emb.Values is null || emb.Values.Count != EmbeddingDimensions)
                {
                    throw new InvalidOperationException(ApiMessages.InvalidEmbeddingVector);
                }
                allResults.Add(emb.Values.ToArray());
            }
        }

        return allResults;
    }

    // ─── Microsoft.Extensions.AI IEmbeddingGenerator Implementation ───────────

    public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var inputList = values as IReadOnlyList<string> ?? values.ToList();
        var vectors = await CreateBatchEmbeddingsAsync(inputList, cancellationToken);

        var generated = new GeneratedEmbeddings<Embedding<float>>(
            vectors.Select(v => new Embedding<float>(v)));

        return generated;
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return serviceType.IsInstanceOfType(this) ? this : null;
    }

    public void Dispose()
    {
        // HttpClient lifecycle is managed by DI (IHttpClientFactory)
    }

    private void EnsureConfigured()
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException(ApiMessages.EmbeddingServiceNotConfigured);
        }
    }

    // ─── JSON Transfer Records ────────────────────────────────────────────────

    private sealed record GeminiEmbeddingRequest(
        GeminiContent Content,
        [property: JsonPropertyName("outputDimensionality")] int OutputDimensionality);

    private sealed record GeminiBatchEmbeddingRequest(
        List<GeminiBatchEntry> Requests);

    private sealed record GeminiBatchEntry(
        string Model,
        GeminiContent Content,
        [property: JsonPropertyName("outputDimensionality")] int OutputDimensionality);

    private sealed record GeminiContent(List<GeminiPart> Parts);
    private sealed record GeminiPart(string Text);
    private sealed record GeminiEmbeddingResponse(GeminiEmbedding? Embedding);
    private sealed record GeminiBatchEmbeddingResponse(List<GeminiEmbedding>? Embeddings);
    private sealed record GeminiEmbedding([property: JsonPropertyName("values")] List<float>? Values);
}
