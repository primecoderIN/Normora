using System.Text.Json.Serialization;

namespace Normora.Modules.Conversations.Infrastructure.Llm;

internal sealed record GeminiGenerateRequest(
    [property: JsonPropertyName("contents")] List<GeminiContent> Contents,
    [property: JsonPropertyName("generationConfig")] GeminiGenerationConfig GenerationConfig,
    [property: JsonPropertyName("tools")] List<object>? Tools = null);

internal sealed record GeminiContent
{
    [JsonPropertyName("parts")]
    public List<GeminiPart> Parts { get; init; } = [];

    [JsonPropertyName("role")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Role { get; init; }

    public GeminiContent() { }
    public GeminiContent(List<GeminiPart> parts, string? role = null)
    {
        Parts = parts;
        Role = role;
    }
}

internal sealed record GeminiPart
{
    [JsonPropertyName("text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Text { get; init; }

    [JsonPropertyName("functionCall")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public GeminiFunctionCall? FunctionCall { get; init; }

    [JsonPropertyName("functionResponse")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public GeminiFunctionResponse? FunctionResponse { get; init; }

    public GeminiPart() { }
    public GeminiPart(string text) => Text = text;
}

internal sealed record GeminiFunctionCall(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("args")] System.Text.Json.JsonElement? Args);

internal sealed record GeminiFunctionResponse(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("response")] object Response);

internal sealed record GeminiGenerationConfig(
    [property: JsonPropertyName("temperature")] double Temperature);

internal sealed record GeminiGenerateResponse(
    [property: JsonPropertyName("candidates")] List<GeminiCandidate>? Candidates);

internal sealed record GeminiCandidate(
    [property: JsonPropertyName("content")] GeminiContent? Content);

