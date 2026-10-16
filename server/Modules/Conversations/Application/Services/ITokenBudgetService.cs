namespace Normora.Modules.Conversations.Application.Services;

/// <summary>
/// Estimates token usage and trims conversation history to fit within a token budget.
/// Uses the universal heuristic of 1 token ≈ 4 characters, which is accurate enough
/// for English prose without requiring a full tokenizer dependency.
/// </summary>
public interface ITokenBudgetService
{
    /// <summary>
    /// Estimates the number of tokens in a piece of text using the 4-chars-per-token heuristic.
    /// </summary>
    /// <param name="text">The text to estimate tokens for.</param>
    /// <returns>Estimated token count.</returns>
    int EstimateTokenCount(string text);

    /// <summary>
    /// Trims conversation history (oldest first) to fit the most recent messages within the given
    /// token budget. Always retains at least the most recent message, even if it alone exceeds budget.
    /// </summary>
    /// <param name="history">Full conversation history in chronological order.</param>
    /// <param name="tokenBudget">Maximum token budget for the returned history slice.</param>
    /// <returns>A trimmed, still-chronological slice of the history.</returns>
    IReadOnlyList<ConversationMessageContext> TrimToTokenBudget(
        IReadOnlyList<ConversationMessageContext> history,
        int tokenBudget);
}
