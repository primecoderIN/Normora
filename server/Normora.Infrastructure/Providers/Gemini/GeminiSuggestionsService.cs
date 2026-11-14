using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Normora.Modules.Conversations.Application.Services;

namespace Normora.Infrastructure.Providers.Gemini;

/// <summary>
/// Calls <see cref="IChatClient"/> to generate question suggestions based on accessible document names.
/// Uses standardized AI client abstractions without direct HTTP or credential handling.
/// </summary>
public sealed class GeminiSuggestionsService(
    IChatClient chatClient,
    ILogger<GeminiSuggestionsService> logger) : ISuggestionsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<IReadOnlyList<string>> GenerateSuggestionsAsync(
        IReadOnlyList<string> documentNames,
        CancellationToken cancellationToken = default)
    {
        if (documentNames.Count == 0)
        {
            return [];
        }

        var documentList = string.Join("\n", documentNames.Select(n => $"- {n}"));

        var prompt = $"""
            You are an assistant helping employees explore their company's document knowledge base.
            Based on the following list of company document names, generate exactly 5 short, natural-language questions that an employee might realistically ask.
            Each question should be directly answerable from the kind of content those documents would contain.
            
            Rules:
            - Return ONLY a valid JSON array of 5 strings. No markdown, no explanation, no wrapper.
            - Each string is a complete question (15–60 characters).
            - Do NOT number the questions.
            - Questions should be varied and cover different document topics.
            
            Company documents:
            {documentList}
            
            JSON array:
            """;

        try
        {
            var options = new ChatOptions { Temperature = 0.2f };
            var response = await chatClient.GetResponseAsync([new ChatMessage(ChatRole.User, prompt)], options, cancellationToken);
            var raw = response.Text?.Trim();

            if (string.IsNullOrWhiteSpace(raw))
            {
                logger.LogWarning("LLM returned an empty suggestions response.");
                return [];
            }

            // Strip markdown code fences if wrapped
            if (raw.StartsWith("```"))
            {
                raw = raw.Split('\n').Skip(1).TakeWhile(l => !l.StartsWith("```")).Aggregate((a, b) => $"{a}\n{b}");
            }

            var parsed = JsonSerializer.Deserialize<List<string>>(raw, JsonOptions);
            return parsed?
                .Where(q => !string.IsNullOrWhiteSpace(q))
                .Take(5)
                .ToList() ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate suggestions from LLM chat client.");
            return [];
        }
    }
}
