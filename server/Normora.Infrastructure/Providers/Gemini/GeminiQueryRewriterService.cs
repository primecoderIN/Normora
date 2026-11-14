using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Normora.Modules.Conversations.Application.Services;

namespace Normora.Infrastructure.Providers.Gemini;

/// <summary>
/// Adapts the standardized <see cref="IChatClient"/> to the application query-rewriting contract.
/// Rewrites follow-up conversation turns into standalone retrieval queries for vector similarity search.
/// </summary>
public sealed class GeminiQueryRewriterService(
    IChatClient chatClient,
    ILogger<GeminiQueryRewriterService> logger) : IQueryRewriterService
{
    public async Task<string> RewriteQueryAsync(
        string currentQuestion,
        IReadOnlyList<ConversationMessageContext> conversationHistory,
        CancellationToken cancellationToken = default)
    {
        // Skip rewriting if this is the very first question since there's no prior context
        if (conversationHistory.Count == 0)
        {
            return currentQuestion;
        }

        var historyText = string.Join("\n", conversationHistory.Select(m => $"{m.Role}: {m.Content}"));

        var prompt = $"""
            Given the following conversation history and a follow-up user question, rephrase the follow-up question to be a standalone question that can be used to query a vector database.
            Do NOT answer the question. Only return the standalone question. If the question does not need rewriting, return it exactly as is.

            Conversation History:
            {historyText}

            Follow-up Question:
            {currentQuestion}

            Standalone Question:
            """;

        var options = new ChatOptions { Temperature = 0.0f };
        var response = await chatClient.GetResponseAsync([new ChatMessage(ChatRole.User, prompt)], options, cancellationToken);
        var rewritten = response.Text;

        logger.LogDebug("Query rewritten from '{Original}' to '{Rewritten}'", currentQuestion, rewritten);
        return string.IsNullOrWhiteSpace(rewritten) ? currentQuestion : rewritten.Trim();
    }
}
