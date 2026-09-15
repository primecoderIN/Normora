namespace Normora.Api.Features.Ask;

/// <summary>
/// Defines the contract for generating natural language answers based on retrieved document chunks.
/// </summary>
public interface ITextGenerationService
{
    /// <summary>
    /// Gets a value indicating whether the generation service is properly configured (e.g., API keys exist).
    /// </summary>
    bool IsConfigured { get; }


    /// <summary>
    /// Generates a grounded, multi-turn answer that embeds prior conversation turns into the prompt,
    /// enabling the LLM to resolve pronouns, follow-ups, and references to prior answers.
    /// Used by <c>AskConversationCommand</c> for conversational RAG.
    /// </summary>
    /// <param name="question">The current (possibly rewritten) question.</param>
    /// <param name="sources">Retrieved document chunks as grounding evidence.</param>
    /// <param name="history">Prior conversation turns (user + assistant), oldest-first, already token-budgeted.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A context-aware grounded answer.</returns>
    Task<string> GenerateConversationalAnswerAsync(
        string question,
        IReadOnlyList<AskSource> sources,
        IReadOnlyList<ConversationTurn> history,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams a grounded, multi-turn answer back as an async enumerable of text chunks.
    /// Used by <c>AskConversationStreamCommand</c> for streaming conversational RAG.
    /// </summary>
    IAsyncEnumerable<string> StreamConversationalAnswerAsync(
        string question,
        IReadOnlyList<AskSource> sources,
        IReadOnlyList<ConversationTurn> history,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a concise title (≤ 6 words) for a conversation based on its first user question.
    /// Called once after the first assistant reply is persisted.
    /// </summary>
    /// <param name="firstQuestion">The first question the user asked in this conversation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A short, human-readable conversation title.</returns>
    Task<string> GenerateTitleAsync(
        string firstQuestion,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// A prior turn in the conversation: the user's question and the assistant's answer.
/// Used to provide multi-turn context when generating the next answer.
/// </summary>
/// <param name="UserQuestion">The user's question in this turn.</param>
/// <param name="AssistantAnswer">The assistant's answer in this turn.</param>
public sealed record ConversationTurn(string UserQuestion, string AssistantAnswer);

/// <summary>
/// Represents a specific chunk of text from a document used as evidence for text generation.
/// </summary>
/// <param name="FileName">The name of the source document.</param>
/// <param name="ChunkIndex">The zero-based index of this chunk within the document.</param>
/// <param name="Content">The raw extracted text of the chunk.</param>
public sealed record AskSource(string FileName, int ChunkIndex, string Content);