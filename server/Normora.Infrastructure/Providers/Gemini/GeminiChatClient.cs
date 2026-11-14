using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Normora.Shared.Constants;

namespace Normora.Infrastructure.Providers.Gemini;

/// <summary>
/// Native Google Gemini implementation of <see cref="IChatClient"/> from Microsoft.Extensions.AI.
/// Standardizes conversational completions, streaming, and tool bindings across Normora's infrastructure.
/// </summary>
public sealed class GeminiChatClient(HttpClient httpClient, IOptions<GeminiOptions> options) : IChatClient
{
    private readonly string? _apiKey = options.Value.ApiKey;
    private readonly string _model = options.Value.GenerationModel;

    public ChatClientMetadata Metadata => new(
        providerName: "Gemini",
        providerUri: new Uri("https://generativelanguage.googleapis.com"),
        defaultModelId: _model);

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var contents = ConvertMessages(chatMessages);
        var temperature = options?.Temperature ?? 0.1;
        var request = new GeminiGenerateRequest(contents, new GeminiGenerationConfig(temperature));

        using var httpRequest = CreateRequest(HttpMethod.Post, $"models/{_model}:generateContent", request);
        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<GeminiGenerateResponse>(cancellationToken);
        var text = result?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? string.Empty;

        return new ChatResponse([new ChatMessage(ChatRole.Assistant, text.Trim())])
        {
            ModelId = _model
        };
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var contents = ConvertMessages(chatMessages);
        var temperature = options?.Temperature ?? 0.1;
        var request = new GeminiGenerateRequest(contents, new GeminiGenerationConfig(temperature));

        using var httpRequest = CreateRequest(HttpMethod.Post, $"models/{_model}:streamGenerateContent?alt=sse", request);
        using var response = await httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
        {
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: ")) continue;

            var json = line["data: ".Length..];
            GeminiGenerateResponse? result = null;
            try
            {
                result = System.Text.Json.JsonSerializer.Deserialize<GeminiGenerateResponse>(json);
            }
            catch
            {
                // Ignore partial JSON parse errors between chunk buffers
            }

            var text = result?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
            if (!string.IsNullOrEmpty(text))
            {
                yield return new ChatResponseUpdate(ChatRole.Assistant, text);
            }
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return serviceType.IsInstanceOfType(this) ? this : null;
    }

    public void Dispose()
    {
        // HttpClient lifecycle is managed by DI
    }

    private void EnsureConfigured()
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException(ApiMessages.LlmGenerationNotConfigured);
        }
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string path, object? body = null)
    {
        var request = new HttpRequestMessage(method, path);
        if (!string.IsNullOrWhiteSpace(_apiKey))
        {
            request.Headers.Add("x-goog-api-key", _apiKey);
        }
        if (body != null)
        {
            request.Content = JsonContent.Create(body);
        }
        return request;
    }

    private static List<GeminiContent> ConvertMessages(IEnumerable<ChatMessage> chatMessages)
    {
        var contents = new List<GeminiContent>();
        foreach (var msg in chatMessages)
        {
            var role = msg.Role == ChatRole.Assistant ? "model" : "user";
            contents.Add(new GeminiContent([new GeminiPart(msg.Text ?? string.Empty)], role));
        }
        return contents;
    }

    // ─── JSON Transfer Models ──────────────────────────────────────────────────

    private sealed record GeminiGenerateRequest(
        List<GeminiContent> Contents,
        GeminiGenerationConfig? GenerationConfig = null);

    private sealed record GeminiGenerationConfig(
        [property: JsonPropertyName("temperature")] double Temperature);

    private sealed record GeminiContent(
        List<GeminiPart> Parts,
        [property: JsonPropertyName("role")] string? Role = null);

    private sealed record GeminiPart(
        [property: JsonPropertyName("text")] string? Text);

    private sealed record GeminiGenerateResponse(
        List<GeminiCandidate>? Candidates);

    private sealed record GeminiCandidate(
        GeminiContent? Content);
}
