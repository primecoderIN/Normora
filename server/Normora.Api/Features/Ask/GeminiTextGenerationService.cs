using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Normora.Shared.Options;

namespace Normora.Api.Features.Ask;

/// <summary>
/// Implementation of <see cref="ITextGenerationService"/> that uses Google's Gemini API.
/// Supports both stateless grounded answering and multi-turn conversational RAG.
/// </summary>
public sealed class GeminiTextGenerationService : ITextGenerationService
{
    private readonly HttpClient httpClient;
    private readonly string? _apiKey;
    private readonly string _model;

    public GeminiTextGenerationService(HttpClient httpClient, IOptions<GeminiOptions> options)
    {
        this.httpClient = httpClient;
        var geminiOptions = options.Value;
        this._apiKey = geminiOptions.ApiKey;
        this._model = geminiOptions.GenerationModel;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);



    // ─── Multi-turn conversational answer (used by AskConversationCommand) ─────

    /// <inheritdoc />
    public async Task<string> GenerateConversationalAnswerAsync(
        string question,
        IReadOnlyList<AskSource> sources,
        IReadOnlyList<ConversationTurn> history,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var sourcesBlock = BuildSourcesBlock(sources);
        var historyBlock = BuildHistoryBlock(history);

        // Build a strict system prompt instructing the LLM to only use the provided company documents and never invent answers
        var prompt = new StringBuilder();
        prompt.AppendLine("""
            You are Normora, a company policy assistant having an ongoing conversation with an employee.
            Answer using ONLY the provided company document sources below. Do not invent policies, numbers, dates, or exceptions.
            If the sources do not contain the answer, say exactly: I could not find that in the company documents.
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

        return await CallGeminiAsync(prompt.ToString(), temperature: 0.1, cancellationToken);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> StreamConversationalAnswerAsync(
        string question,
        IReadOnlyList<AskSource> sources,
        IReadOnlyList<ConversationTurn> history,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var sourcesBlock = BuildSourcesBlock(sources);
        var historyBlock = BuildHistoryBlock(history);

        var prompt = new StringBuilder();
        prompt.AppendLine("""
            You are Normora, a company policy assistant having an ongoing conversation with an employee.
            Answer using ONLY the provided company document sources below. Do not invent policies, numbers, dates, or exceptions.
            If the sources do not contain the answer, say exactly: I could not find that in the company documents.
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

        var anyChunks = false;
        await foreach (var chunk in StreamGeminiAsync(prompt.ToString(), temperature: 0.1, cancellationToken))
        {
            anyChunks = true;
            yield return chunk;
        }

        if (!anyChunks)
        {
            yield return "I could not find that in the company documents.";
        }
    }

    // ─── Auto-title ─────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<string> GenerateTitleAsync(
        string firstQuestion,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        // Prompt the LLM to generate a short, clean title summarizing the user's initial question so we can label their chat history
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

        // Sanitise: strip quotes, trim, enforce 6-word limit as a safety net
        title = title.Trim().Trim('"', '\'');
        var words = title.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words.Length > 6 ? string.Join(' ', words[..6]) : title;
    }

    // ─── Shared helpers ──────────────────────────────────────────────────────────

    private static string BuildSourcesBlock(IReadOnlyList<AskSource> sources) =>
        string.Join(
            "\n\n",
            sources.Select((s, i) => $"[Source {i + 1}: {s.FileName}, chunk {s.ChunkIndex}]\n{s.Content}"));

    private static string BuildHistoryBlock(IReadOnlyList<ConversationTurn> history)
    {
        if (history.Count == 0) return string.Empty;

        var sb = new StringBuilder();
        foreach (var turn in history)
        {
            sb.AppendLine($"[Employee]: {turn.UserQuestion}");
            sb.AppendLine($"[Normora]: {turn.AssistantAnswer}");
        }
        return sb.ToString().TrimEnd();
    }

    private async Task<string> CallGeminiAsync(
        string prompt,
        double temperature,
        CancellationToken cancellationToken)
    {
        var request = new GeminiGenerateRequest(
            [new GeminiContent([new GeminiPart(prompt)])],
            new GeminiGenerationConfig(temperature));

        using var response = await httpClient.PostAsJsonAsync(
            $"models/{_model}:generateContent?key={_apiKey}",
            request,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<GeminiGenerateResponse>(cancellationToken);
        var text = result?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
        return string.IsNullOrWhiteSpace(text)
            ? "I could not find that in the company documents."
            : text.Trim();
    }

    private void EnsureConfigured()
    {
        if (!IsConfigured)
            throw new InvalidOperationException("Ask Normora requires Gemini generation to be configured.");
    }

    private async IAsyncEnumerable<string> StreamGeminiAsync(
        string prompt,
        double temperature,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var request = new GeminiGenerateRequest(
            [new GeminiContent([new GeminiPart(prompt)])],
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
        [property: JsonPropertyName("generationConfig")] GeminiGenerationConfig GenerationConfig);

    private sealed record GeminiContent(
        [property: JsonPropertyName("parts")] List<GeminiPart> Parts);
    private sealed record GeminiPart(
        [property: JsonPropertyName("text")] string Text);
    private sealed record GeminiGenerationConfig(
        [property: JsonPropertyName("temperature")] double Temperature);
    private sealed record GeminiGenerateResponse(
        [property: JsonPropertyName("candidates")] List<GeminiCandidate>? Candidates);
    private sealed record GeminiCandidate(
        [property: JsonPropertyName("content")] GeminiContent? Content);
}