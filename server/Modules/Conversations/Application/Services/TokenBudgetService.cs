namespace Normora.Modules.Conversations.Application.Services;

/// <summary>
/// Stateless, allocation-efficient token estimator and sliding-window history trimmer.
/// Uses the 1 token ≈ 4 characters heuristic — accurate enough for English without
/// requiring a tokenizer library dependency.
/// </summary>
public sealed class TokenBudgetService : ITokenBudgetService
{
    // Standard heuristic: 1 token ≈ 4 characters for English text
    private const int CharsPerToken = 4;

    // Per-message overhead: role label, separators, and newline characters
    private const int MessageOverheadTokens = 5;

    /// <inheritdoc />
    // Quickly estimate the token count using the 1 token ≈ 4 characters heuristic instead of pulling in a heavy tokenizer library
    public int EstimateTokenCount(string text) =>
        string.IsNullOrEmpty(text) ? 0 : (int)Math.Ceiling(text.Length / (double)CharsPerToken);

    /// <inheritdoc />
    public IReadOnlyList<ConversationMessageContext> TrimToTokenBudget(
        IReadOnlyList<ConversationMessageContext> history,
        int tokenBudget)
    {
        if (history.Count == 0 || tokenBudget <= 0)
            return history;

        // Walk backwards from the most recent message, collecting history until the token limit is hit so we don't overwhelm the LLM
        // Using LinkedList<T> so AddFirst is O(1) and we get chronological order for free.
        var result = new LinkedList<ConversationMessageContext>();
        int used = 0;

        for (int i = history.Count - 1; i >= 0; i--)
        {
            var msg = history[i];
            int cost = EstimateTokenCount(msg.Content) + MessageOverheadTokens;

            // Stop if we'd exceed the budget — but always keep at least the most-recent message
            // so callers always receive something useful.
            if (used + cost > tokenBudget && result.Count > 0)
                break;

            result.AddFirst(msg);
            used += cost;
        }

        return [.. result];
    }
}
