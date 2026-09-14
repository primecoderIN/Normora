using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;

namespace Normora.Api.Features.Ask;

/// <summary>
/// Implementation of <see cref="ITextGenerationService"/> that uses Google's Gemini API.
/// Supports both stateless grounded answering and multi-turn conversational RAG.
/// </summary>
public sealed class GeminiTextGenerationService(
    HttpClient httpClient,
    IConfiguration configuration) : ITextGenerationService
{
    private readonly string? _apiKey = configuration["Gemini:ApiKey"];
    private readonly string _model = configuration["Gemini:GenerationModel"] ?? "gemini-2.0-flash";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

    // ─── Stateless grounded answer (used by AskQuestionQuery) ──────────────────

    /// <inheritdoc />
    public async Task<string> GenerateGroundedAnswerAsync(
        string question,
        IReadOnlyList<AskSource> sources,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var context = BuildSourcesBlock(sources);
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

        return await CallGeminiAsync(prompt, temperature: 0.1, cancellationToken);
    }

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

    // ─── Gemini JSON DTOs ────────────────────────────────────────────────────────

    private sealed record GeminiGenerateRequest(
        List<GeminiContent> Contents,
        [property: JsonPropertyName("generationConfig")] GeminiGenerationConfig GenerationConfig);

    private sealed record GeminiContent(List<GeminiPart> Parts);
    private sealed record GeminiPart(string Text);
    private sealed record GeminiGenerationConfig(
        [property: JsonPropertyName("temperature")] double Temperature);
    private sealed record GeminiGenerateResponse(List<GeminiCandidate>? Candidates);
    private sealed record GeminiCandidate(GeminiContent? Content);
}