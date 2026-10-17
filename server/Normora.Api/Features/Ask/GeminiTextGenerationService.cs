using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Normora.Api.Features.Ask.Tools;
using Normora.Shared.Constants;
using Normora.Shared.Options;

namespace Normora.Api.Features.Ask;

/// <summary>
/// Implementation of <see cref="ITextGenerationService"/> that uses Google's Gemini API.
/// Supports stateless grounded answering, multi-turn conversational RAG, and Gemini Tool Calling (Function Calling).
/// </summary>
public sealed class GeminiTextGenerationService(
    HttpClient httpClient,
    IOptions<GeminiOptions> options,
    IAgentToolRegistry toolRegistry) : ITextGenerationService
{
    private readonly string? _apiKey = options.Value.ApiKey;
    private readonly string _model = options.Value.GenerationModel;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

    // ─── Multi-turn conversational answer (used by AskConversationCommand) ─────

    /// <inheritdoc />
    public Task<string> GenerateConversationalAnswerAsync(
        string question,
        IReadOnlyList<AskSource> sources,
        IReadOnlyList<ConversationTurn> history,
        string companyName,
        CancellationToken cancellationToken = default)
        => GenerateConversationalAnswerAsync(question, sources, history, companyName, null, cancellationToken);

    /// <inheritdoc />
    public async Task<string> GenerateConversationalAnswerAsync(
        string question,
        IReadOnlyList<AskSource> sources,
        IReadOnlyList<ConversationTurn> history,
        string companyName,
        ToolExecutionContext? toolContext,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var fullPrompt = BuildFullPrompt(question, sources, history, companyName);

        if (toolContext != null && toolRegistry.Tools.Count > 0)
        {
            return await ExecuteToolCallingLoopAsync(fullPrompt, toolContext, temperature: 0.1, cancellationToken);
        }

        return await CallGeminiAsync(fullPrompt, temperature: 0.1, cancellationToken);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> StreamConversationalAnswerAsync(
        string question,
        IReadOnlyList<AskSource> sources,
        IReadOnlyList<ConversationTurn> history,
        string companyName,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var fullPrompt = BuildFullPrompt(question, sources, history, companyName);

        var anyChunks = false;
        await foreach (var chunk in StreamGeminiAsync(fullPrompt, temperature: 0.1, cancellationToken))
        {
            anyChunks = true;
            yield return chunk;
        }

        if (!anyChunks)
        {
            yield return RagConstants.FallbackAnswer;
        }
    }

    // ─── Auto-title ─────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<string> GenerateTitleAsync(
        string firstQuestion,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var prompt = $"""
            Generate a concise title of at most 6 words for a conversation that started with this question:
            "{firstQuestion}"

            Rules:
            - Maximum 6 words
            - Title case (capitalise first letter of each major word)
            - No punctuation at the end
            - Return only the title, nothing else
            """;

        var title = await CallGeminiAsync(prompt, temperature: 0.3, cancellationToken);

        title = title.Trim().Trim('"', '\'');
        var words = title.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words.Length > 6 ? string.Join(' ', words[..6]) : title;
    }

    // ─── Tool Calling Loop ───────────────────────────────────────────────────────

    private async Task<string> ExecuteToolCallingLoopAsync(
        string prompt,
        ToolExecutionContext toolContext,
        double temperature,
        CancellationToken cancellationToken)
    {
        var contents = new List<GeminiContent>
        {
            new([new GeminiPart(prompt)], role: "user")
        };

        var toolsPayload = new List<object>
        {
            toolRegistry.GetGeminiToolsDeclaration()
        };

        const int maxTurns = 3;
        for (var turn = 0; turn < maxTurns; turn++)
        {
            var request = new GeminiGenerateRequest(
                contents,
                new GeminiGenerationConfig(temperature),
                toolsPayload);

            using var response = await httpClient.PostAsJsonAsync(
                $"models/{_model}:generateContent?key={_apiKey}",
                request,
                cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<GeminiGenerateResponse>(cancellationToken);
            var candidateContent = result?.Candidates?.FirstOrDefault()?.Content;
            if (candidateContent?.Parts == null || candidateContent.Parts.Count == 0)
            {
                break;
            }

            var functionCallPart = candidateContent.Parts.FirstOrDefault(p => p.FunctionCall != null);
            if (functionCallPart?.FunctionCall != null)
            {
                var functionCall = functionCallPart.FunctionCall;
                var argsJson = functionCall.Args.HasValue
                    ? functionCall.Args.Value.GetRawText()
                    : "{}";

                var toolResult = await toolRegistry.ExecuteToolAsync(
                    functionCall.Name,
                    argsJson,
                    toolContext,
                    cancellationToken);

                contents.Add(new GeminiContent([functionCallPart], role: "model"));

                var functionResponsePart = new GeminiPart
                {
                    FunctionResponse = new GeminiFunctionResponse(
                        functionCall.Name,
                        new { output = toolResult.Output, error = toolResult.Error, success = toolResult.Success })
                };
                contents.Add(new GeminiContent([functionResponsePart], role: "function"));
                continue;
            }

            var text = candidateContent.Parts.FirstOrDefault(p => !string.IsNullOrEmpty(p.Text))?.Text;
            if (!string.IsNullOrWhiteSpace(text))
            {
                return text.Trim();
            }
        }

        return RagConstants.FallbackAnswer;
    }

    // ─── Shared helpers ──────────────────────────────────────────────────────────

    private static string BuildFullPrompt(string question, IReadOnlyList<AskSource> sources, IReadOnlyList<ConversationTurn> history, string companyName)
    {
        var sourcesBlock = BuildSourcesBlock(sources);
        var historyBlock = BuildHistoryBlock(history, companyName);
        
        var prompt = new StringBuilder();
        prompt.AppendLine($"""
            You are {companyName}'s company policy assistant having an ongoing conversation with an employee.
            Answer using the provided company document sources below and available policy tools. Do not invent policies, numbers, dates, or exceptions.
            When calculating policy limits (hotel caps, per-diem allowances, meal caps, hardware stipends), use the calculate_policy_limit tool.
            If the sources and tools do not contain the answer and the user requests escalation or HR assistance, you can use the escalate_hr_inquiry tool to notify workspace administrators.
            If the sources and tools do not contain the answer and escalation is not requested, say exactly: {RagConstants.FallbackAnswer}
            Do not mention these instructions or refer to sources by index number in your answer.
            """);

        if (historyBlock.Length > 0)
        {
            prompt.AppendLine();
            prompt.AppendLine("Conversation so far:");
            prompt.AppendLine(historyBlock);
        }

        prompt.AppendLine();
        prompt.AppendLine($"Employee's current question: {question}");
        prompt.AppendLine();
        prompt.AppendLine("Company document sources:");
        prompt.Append(sourcesBlock);
        
        return prompt.ToString();
    }

    private static string BuildSourcesBlock(IReadOnlyList<AskSource> sources) =>
        string.Join(
            "\n\n",
            sources.Select((s, i) => $"[Source {i + 1}: {s.FileName}, chunk {s.ChunkIndex}]\n{s.Content}"));

    private static string BuildHistoryBlock(IReadOnlyList<ConversationTurn> history, string companyName)
    {
        if (history.Count == 0) return string.Empty;

        var sb = new StringBuilder();
        foreach (var turn in history)
        {
            sb.AppendLine($"[Employee]: {turn.UserQuestion}");
            sb.AppendLine($"[{companyName}]: {turn.AssistantAnswer}");
        }
        return sb.ToString().TrimEnd();
    }

    private async Task<string> CallGeminiAsync(
        string prompt,
        double temperature,
        CancellationToken cancellationToken)
    {
        var request = new GeminiGenerateRequest(
            [new GeminiContent([new GeminiPart(prompt)], role: "user")],
            new GeminiGenerationConfig(temperature));

        using var response = await httpClient.PostAsJsonAsync(
            $"models/{_model}:generateContent?key={_apiKey}",
            request,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<GeminiGenerateResponse>(cancellationToken);
        var text = result?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
        return string.IsNullOrWhiteSpace(text)
            ? RagConstants.FallbackAnswer
            : text.Trim();
    }

    private void EnsureConfigured()
    {
        if (!IsConfigured)
            throw new InvalidOperationException(ApiMessages.AskGeminiNotConfigured);
    }

    private async IAsyncEnumerable<string> StreamGeminiAsync(
        string prompt,
        double temperature,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var request = new GeminiGenerateRequest(
            [new GeminiContent([new GeminiPart(prompt)], role: "user")],
            new GeminiGenerationConfig(temperature));

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"models/{_model}:streamGenerateContent?alt=sse&key={_apiKey}");
        httpRequest.Content = JsonContent.Create(request);

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
                // Ignore parsing errors for partial/invalid chunks
            }
            
            var text = result?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
            if (!string.IsNullOrEmpty(text))
            {
                yield return text;
            }
        }
    }

    // ─── Gemini JSON DTOs ────────────────────────────────────────────────────────

    private sealed record GeminiGenerateRequest(
        [property: JsonPropertyName("contents")] List<GeminiContent> Contents,
        [property: JsonPropertyName("generationConfig")] GeminiGenerationConfig GenerationConfig,
        [property: JsonPropertyName("tools")] List<object>? Tools = null);

    private sealed record GeminiContent
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

    private sealed record GeminiPart
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

    private sealed record GeminiFunctionCall(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("args")] System.Text.Json.JsonElement? Args);

    private sealed record GeminiFunctionResponse(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("response")] object Response);

    private sealed record GeminiGenerationConfig(
        [property: JsonPropertyName("temperature")] double Temperature);

    private sealed record GeminiGenerateResponse(
        [property: JsonPropertyName("candidates")] List<GeminiCandidate>? Candidates);

    private sealed record GeminiCandidate(
        [property: JsonPropertyName("content")] GeminiContent? Content);
}