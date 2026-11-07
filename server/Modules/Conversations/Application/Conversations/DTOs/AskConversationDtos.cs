namespace Normora.Modules.Conversations.Application.Conversations.DTOs;

/// <summary>
/// Result of an ask conversation turn containing the persisted message IDs, answer text, and source citations.
/// </summary>
public sealed record AskConversationResult(
    Guid ConversationId,
    Guid UserMessageId,
    Guid AssistantMessageId,
    string Answer,
    IReadOnlyList<AskConversationCitation> Sources);

/// <summary>
/// Citation reference indicating a specific document chunk used to ground the conversational answer.
/// </summary>
public sealed record AskConversationCitation(
    Guid DocumentId,
    string FileName,
    int ChunkIndex,
    double Score);

