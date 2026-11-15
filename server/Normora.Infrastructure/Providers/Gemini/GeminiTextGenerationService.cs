using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
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
    IAgentToolRegistry toolRegistry,
    IActionCardFactory actionCardFactory,
    IAgentApprovalService? approvalService = null,
    IWorkspaceStateSynchronizer? stateSynchronizer = null,
    ILogger<GeminiTextGenerationService>? logger = null) : ITextGenerationService
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

            using var response = await GeminiRetryHelper.SendWithRetryAsync(
                httpClient,
                () => CreateGeminiRequest(HttpMethod.Post, $"models/{_model}:generateContent", request),
                cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                return ApiMessages.RateLimitExceeded;
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                logger?.LogError("Gemini API call failed with status {StatusCode}: {ErrorBody}", response.StatusCode, errorBody);
                return ApiMessages.AiServiceUnavailable;
            }

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

                var targetTool = toolRegistry.FindTool(functionCall.Name);
                if (targetTool != null && targetTool.RequiresApproval)
                {
                    if (approvalService != null)
                    {
                        await approvalService.RequestApprovalAsync(
                            toolContext.TenantId,
                            toolContext.UserId ?? string.Empty,
                            toolContext.ConversationId,
                            targetTool.Name,
                            argsJson,
                            null,
                            null,
                            cancellationToken);
                    }

                    return $"I have prepared the `{functionCall.Name.Replace('_', ' ')}` action for you. Because this operation requires explicit human confirmation, please review and approve it using the action card below.";
                }

                var toolResult = await toolRegistry.ExecuteToolAsync(
                    functionCall.Name,
                    argsJson,
                    toolContext,
                    cancellationToken);

                if (functionCall.Name == "workspace_request_form")
                {
                    return "Please provide the required details in the form below so I can proceed with this request.";
                }

                contents.Add(new GeminiContent([functionCallPart], role: "model"));

                var functionResponsePart = new GeminiPart
                {
                    FunctionResponse = new GeminiFunctionResponse(
                        functionCall.Name,
                        new { output = toolResult.Output, error = toolResult.Error, success = toolResult.Success })
                };
                contents.Add(new GeminiContent([functionResponsePart], role: "user"));
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

            using var response = await GeminiRetryHelper.SendWithRetryAsync(
                httpClient,
                () => CreateGeminiRequest(HttpMethod.Post, $"models/{_model}:generateContent", request),
                cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                yield return new TextDeltaItem(ApiMessages.RateLimitExceeded);
                yield break;
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                logger?.LogError("Gemini API call failed with status {StatusCode}: {ErrorBody}", response.StatusCode, errorBody);
                yield return new TextDeltaItem(ApiMessages.AiServiceUnavailable);
                yield break;
            }

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

                var targetTool = toolRegistry.FindTool(functionCall.Name);
                if (targetTool != null && targetTool.RequiresApproval)
                {
                    Guid? approvalId = null;
                    if (approvalService != null)
                    {
                        approvalId = await approvalService.RequestApprovalAsync(
                            toolContext.TenantId,
                            toolContext.UserId ?? string.Empty,
                            toolContext.ConversationId,
                            targetTool.Name,
                            argsJson,
                            null,
                            null,
                            cancellationToken);
                    }

                    yield return new ToolStartedItem(functionCall.Name, callId);
                    yield return new ToolFinishedItem(
                        functionCall.Name,
                        callId,
                        true,
                        "Authorization required: awaiting confirmation");

                    var pendingResult = new AgentToolResult(
                        Success: true,
                        Output: "{\"status\":\"pending_approval\",\"approvalId\":\"" + (approvalId?.ToString() ?? "") + "\"}",
                        StructuredData: new { approvalId, status = "pending_approval" });

                    var approvalCard = actionCardFactory.CreateCard(functionCall.Name, argsJson, pendingResult);
                    if (approvalCard != null)
                    {
                        yield return approvalCard with { ApprovalId = approvalId?.ToString() };
                    }
                    else
                    {
                        yield return new ActionCardItem(
                            ActionType: "pending_approval_card",
                            Title: $"Approval Required: {functionCall.Name.Replace('_', ' ')}",
                            Description: "This action requires explicit authorization before execution.",
                            Parameters: new Dictionary<string, string>
                            {
                                ["Action"] = functionCall.Name,
                                ["Status"] = "Awaiting Confirmation"
                            },
                            ConfirmButtonText: "Confirm Action",
                            CancelButtonText: "Cancel",
                            ApprovalId: approvalId?.ToString());
                    }

                    yield return new TextDeltaItem($"I have prepared this operation for you. Because `{functionCall.Name.Replace('_', ' ')}` is an impactful action, please review the parameters and confirm below to proceed.");
                    answered = true;
                    break;
                }

                yield return new ToolStartedItem(functionCall.Name, callId);

                var toolResult = await toolRegistry.ExecuteToolAsync(
                    functionCall.Name,
                    argsJson,
                    toolContext,
                    cancellationToken);

                var descriptor = toolRegistry.GetChipDescriptor(functionCall.Name);
                var summary = toolResult.Success
                    ? (!string.IsNullOrWhiteSpace(descriptor?.CompletedLabel)
                        ? descriptor.CompletedLabel
                        : (!string.IsNullOrWhiteSpace(toolResult.Output) && toolResult.Output.Length > 80
                            ? toolResult.Output[..80] + "..."
                            : toolResult.Output))
                    : (!string.IsNullOrWhiteSpace(descriptor?.FailedLabel)
                        ? descriptor.FailedLabel
                        : toolResult.Error);

                yield return new ToolFinishedItem(functionCall.Name, callId, toolResult.Success, summary);

                var card = actionCardFactory.CreateCard(functionCall.Name, argsJson, toolResult);
                if (card != null)
                {
                    yield return card;
                }

                if (toolResult.Success && toolResult.ChangedData && stateSynchronizer != null)
                {
                    var newVersion = await stateSynchronizer.IncrementVersionAsync(toolContext.TenantId, functionCall.Name, cancellationToken);
                    yield return new StateSnapshotItem(newVersion);
                }

                if (functionCall.Name == "workspace_request_form")
                {
                    yield return new TextDeltaItem("Please provide the required details in the form below so I can proceed with this request.");
                    yield break;
                }

                contents.Add(new GeminiContent([functionCallPart], role: "model"));

                var functionResponsePart = new GeminiPart
                {
                    FunctionResponse = new GeminiFunctionResponse(
                        functionCall.Name,
                        new { output = toolResult.Output, error = toolResult.Error, success = toolResult.Success })
                };
                contents.Add(new GeminiContent([functionResponsePart], role: "user"));
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

    // ─── Shared helpers ──────────────────────────────────────────────────────────

    private static string BuildFullPrompt(string question, IReadOnlyList<AskSource> sources, IReadOnlyList<ConversationTurn> history, string companyName)
    {
        var sourcesBlock = BuildSourcesBlock(sources);
        var historyBlock = BuildHistoryBlock(history, companyName);
        
        var prompt = new StringBuilder();
        prompt.AppendLine($"""
            You are {companyName}'s intelligent workspace copilot and policy assistant having an ongoing conversation with an employee or administrator.
            Answer policy questions using the provided company document sources below and available policy tools. Do not invent policies, numbers, dates, or exceptions.
            When calculating policy limits (hotel caps, per-diem allowances, meal caps, hardware stipends), use the calculate_policy_limit tool.
            If the user asks how, where, or requests to upload or add new documents, handbooks, or files into Normora, explain that documents are ingested through the Document Management portal (/employer/documents) where administrators can upload PDF, DOCX, TXT, or images up to 50MB with department permissions and automatic background OCR/embedding, and invoke the documents_upload_guide tool to show them the interactive navigation card.
            If the user asks to navigate, go to, or open any section of the app (dashboard, documents, employees, departments, user groups, branding, audit logs, subscription, conversations, saved answers, profile), invoke the workspace_navigate tool with the appropriate destination.
            If the user asks to view or inspect organizational resources, quotas, branding, or their profile, invoke the matching query tool: workspace_get_subscription_usage, workspace_get_branding, user_get_profile, workspace_get_dashboard_summary, or workspace_get_audit_logs.
            If the user asks to manage, update, or remove departments, groups, or employees, invoke the corresponding management tool. Destructive operations (delete_document, workspace_delete_department, workspace_delete_user_group, workspace_remove_employee, conversations_delete) require user confirmation via human-in-the-loop approval.
            If a tool requires parameters that the user has not provided, invoke the workspace_request_form tool to present a structured intake form.
            If the sources and tools do not contain the answer and the user requests escalation or HR assistance, you can use the escalate_hr_inquiry tool to notify workspace administrators.
            If the question is purely an information query about company policy and neither sources nor tools contain the answer and escalation is not requested, say exactly: {RagConstants.FallbackAnswer}
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
