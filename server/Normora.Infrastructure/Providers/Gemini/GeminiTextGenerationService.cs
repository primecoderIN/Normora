using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Normora.Modules.Conversations.Application.Services;
using Normora.Modules.Conversations.Domain;
using Normora.Shared.Constants;
using Normora.Shared.Interfaces;

namespace Normora.Infrastructure.Providers.Gemini;

/// <summary>
/// Implementation of <see cref="ITextGenerationService"/> that leverages <see cref="IChatClient"/>
/// for conversational answering and streaming, combined with Gemini function calling for workspace agent tools.
/// </summary>
public sealed class GeminiTextGenerationService(
    IChatClient chatClient,
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
    public IAsyncEnumerable<string> StreamConversationalAnswerAsync(
        string question,
        IReadOnlyList<AskSource> sources,
        IReadOnlyList<ConversationTurn> history,
        string companyName,
        CancellationToken cancellationToken = default)
        => StreamConversationalAnswerAsync(question, sources, history, companyName, null, cancellationToken);

    /// <inheritdoc />
    public async IAsyncEnumerable<string> StreamConversationalAnswerAsync(
        string question,
        IReadOnlyList<AskSource> sources,
        IReadOnlyList<ConversationTurn> history,
        string companyName,
        ToolExecutionContext? toolContext,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in StreamConversationalEventsAsync(question, sources, history, companyName, toolContext, cancellationToken))
        {
            if (item is TextDeltaItem delta)
            {
                yield return delta.Text;
            }
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<TextGenerationStreamItem> StreamConversationalEventsAsync(
        string question,
        IReadOnlyList<AskSource> sources,
        IReadOnlyList<ConversationTurn> history,
        string companyName,
        ToolExecutionContext? toolContext,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var fullPrompt = BuildFullPrompt(question, sources, history, companyName);

        // If tools are registered and available in the workspace context, evaluate tool calls first
        if (toolContext != null && toolRegistry.Tools.Count > 0)
        {
            await foreach (var item in ExecuteStreamingToolCallingLoopAsync(fullPrompt, toolContext, temperature: 0.1, cancellationToken))
            {
                yield return item;
            }
            yield break;
        }

        var anyChunks = false;
        await foreach (var chunk in StreamGeminiAsync(fullPrompt, temperature: 0.1, cancellationToken))
        {
            anyChunks = true;
            yield return new TextDeltaItem(chunk);
        }

        if (!anyChunks)
        {
            yield return new TextDeltaItem(RagConstants.FallbackAnswer);
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

    // Executes Gemini Autonomous Function Calling across a multi-turn conversation loop:
    // 1. Injects OpenAPI parameter schemas for registered workspace tools into the Gemini API payload.
    // 2. Intercepts candidate turns where the model requests a function call (e.g., calculate_policy_limit).
    // 3. Executes the corresponding tool in the user's tenant context via IAgentToolRegistry.
    // 4. Feeds the function execution result back into the dialogue history with role 'function'.
    // 5. Continues until Gemini synthesizes a natural language answer or reaches the 3-turn safety bound.
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
            toolRegistry.GetToolDeclarations()
        };

        // Bounded loop ceiling prevents infinite recursive model execution.
        const int maxTurns = 3;
        for (var turn = 0; turn < maxTurns; turn++)
        {
            var request = new GeminiGenerateRequest(
                contents,
                new GeminiGenerationConfig(temperature),
                toolsPayload);

            using var httpRequest = CreateGeminiRequest(HttpMethod.Post, $"models/{_model}:generateContent", request);
            using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
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

    private async IAsyncEnumerable<TextGenerationStreamItem> ExecuteStreamingToolCallingLoopAsync(
        string prompt,
        ToolExecutionContext toolContext,
        double temperature,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var contents = new List<GeminiContent>
        {
            new([new GeminiPart(prompt)], role: "user")
        };

        var toolsPayload = new List<object>
        {
            toolRegistry.GetToolDeclarations()
        };

        const int maxTurns = 3;
        var answered = false;

        for (var turn = 0; turn < maxTurns; turn++)
        {
            var request = new GeminiGenerateRequest(
                contents,
                new GeminiGenerationConfig(temperature),
                toolsPayload);

            using var httpRequest = CreateGeminiRequest(HttpMethod.Post, $"models/{_model}:generateContent", request);
            using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
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
                var callId = Guid.NewGuid().ToString("N")[..8];
                var argsJson = functionCall.Args.HasValue
                    ? functionCall.Args.Value.GetRawText()
                    : "{}";

                yield return new ToolStartedItem(functionCall.Name, callId);

                var toolResult = await toolRegistry.ExecuteToolAsync(
                    functionCall.Name,
                    argsJson,
                    toolContext,
                    cancellationToken);

                var summary = toolResult.Success
                    ? (!string.IsNullOrWhiteSpace(toolResult.Output) && toolResult.Output.Length > 80
                        ? toolResult.Output[..80] + "..."
                        : toolResult.Output)
                    : toolResult.Error;

                yield return new ToolFinishedItem(functionCall.Name, callId, toolResult.Success, summary);

                var card = TryCreateActionCard(functionCall.Name, argsJson, toolResult);
                if (card != null)
                {
                    yield return card;
                }

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
                answered = true;
                yield return new TextDeltaItem(text.Trim());
                yield break;
            }
        }

        if (!answered)
        {
            yield return new TextDeltaItem(RagConstants.FallbackAnswer);
        }
    }

    private static ActionCardItem? TryCreateActionCard(string toolName, string argsJson, AgentToolResult toolResult)
    {
        try
        {
            if (toolName == "calculate_policy_limit" && toolResult.Success && !string.IsNullOrWhiteSpace(toolResult.Output))
            {
                using var doc = JsonDocument.Parse(toolResult.Output);
                var root = doc.RootElement;
                var policy = root.TryGetProperty("policy", out var p) ? p.GetString() : "Policy Limit";
                var tier = root.TryGetProperty("tier", out var t) ? t.GetString() : null;
                var guideline = root.TryGetProperty("guideline", out var g) ? g.GetString() : null;

                var parameters = new Dictionary<string, string>();
                if (tier != null) parameters["Location Tier"] = tier;
                if (root.TryGetProperty("totalMaxAllowanceUsd", out var a))
                    parameters["Total Max Allowance"] = $"${a.GetDecimal():N0}";
                if (root.TryGetProperty("nightlyCapUsd", out var n))
                    parameters["Nightly Cap"] = $"${n.GetDecimal():N0}/night";
                if (root.TryGetProperty("dailyAllowanceUsd", out var d))
                    parameters["Daily Allowance"] = $"${d.GetDecimal():N0}/day";
                if (guideline != null) parameters["Policy Guideline"] = guideline;

                return new ActionCardItem(
                    ActionType: "policy_limit_card",
                    Title: policy ?? "Expense Policy Calculation",
                    Description: $"Deterministic corporate limit calculation for {tier ?? "requested expense"}.",
                    Parameters: parameters,
                    ConfirmButtonText: "Apply Limit",
                    CancelButtonText: "Dismiss");
            }

            if (toolName == "escalate_hr_inquiry")
            {
                using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(argsJson) ? "{}" : argsJson);
                var root = doc.RootElement;
                var inquiry = root.TryGetProperty("inquiry", out var inq) ? inq.GetString() : "Employee Policy Clarification";
                var category = root.TryGetProperty("category", out var cat) ? cat.GetString() : "General";
                var urgency = root.TryGetProperty("urgency", out var urg) ? urg.GetString() : "Normal";

                var parameters = new Dictionary<string, string>
                {
                    ["Inquiry Subject"] = inquiry ?? "Policy Inquiry",
                    ["Department Category"] = category ?? "General",
                    ["Urgency Level"] = urgency ?? "Normal",
                    ["Dispatch Status"] = toolResult.Success ? "Successfully Sent to HR" : "Failed to Send"
                };

                return new ActionCardItem(
                    ActionType: "hr_escalation_card",
                    Title: "HR Administration Escalation",
                    Description: toolResult.Success
                        ? "Notification dispatched to workspace HR administrators for review."
                        : "Escalation notification could not be delivered.",
                    Parameters: parameters,
                    ConfirmButtonText: "View Inquiry Status",
                    CancelButtonText: "Dismiss");
            }
        }
        catch
        {
            // If output parsing fails, fail silently without breaking the stream
        }

        return null;
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
        var chatOptions = new ChatOptions { Temperature = (float)temperature };
        var response = await chatClient.GetResponseAsync([new ChatMessage(ChatRole.User, prompt)], chatOptions, cancellationToken);
        var text = response.Text;
        return string.IsNullOrWhiteSpace(text)
            ? RagConstants.FallbackAnswer
            : text.Trim();
    }

    private void EnsureConfigured()
    {
        if (!IsConfigured)
            throw new InvalidOperationException(ApiMessages.LlmGenerationNotConfigured);
    }

    private HttpRequestMessage CreateGeminiRequest(HttpMethod method, string path, object? body = null)
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

    private async IAsyncEnumerable<string> StreamGeminiAsync(
        string prompt,
        double temperature,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var chatOptions = new ChatOptions { Temperature = (float)temperature };
        await foreach (var update in chatClient.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, prompt)], chatOptions, cancellationToken))
        {
            if (!string.IsNullOrEmpty(update.Text))
            {
                yield return update.Text;
            }
        }
    }
}
