using Normora.Modules.Conversations.Domain;

namespace Normora.Modules.Conversations.Application.Conversations.DTOs;

/// <summary>
/// Data transfer object representing the full conversation thread history including all turns, citations, and ratings.
/// Projected to populate the employee chat viewport.
/// </summary>
public record ConversationDetailDto(
    Guid Id,
    string Title,
    string? Summary,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset LastMessageAt,
    IReadOnlyList<MessageDto> Messages);

/// <summary>
/// Data transfer object representing a single message turn (User inquiry or AI assistant answer).
/// </summary>
public record MessageDto(
    Guid Id,
    MessageRole Role,
    string Content,
    DateTimeOffset CreatedAt,
    bool Rewritten,
    IReadOnlyList<MessageCitationDto> Citations,
    MessageFeedbackRating? Feedback);

/// <summary>
/// Data transfer object representing a ground truth citation reference backing an assistant answer.
/// </summary>
public record MessageCitationDto(
    Guid DocumentId,
    Guid DocumentChunkId,
    string FileName,
    double Score,
    bool IsOutdated);
