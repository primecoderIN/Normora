using Normora.Modules.Conversations.Domain;

namespace Normora.Modules.Conversations.Application.Conversations.DTOs;

public record ConversationDetailDto(
    Guid Id,
    string Title,
    string? Summary,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset LastMessageAt,
    IReadOnlyList<MessageDto> Messages);

public record MessageDto(
    Guid Id,
    MessageRole Role,
    string Content,
    DateTimeOffset CreatedAt,
    bool Rewritten,
    IReadOnlyList<MessageCitationDto> Citations,
    MessageFeedbackRating? Feedback);

public record MessageCitationDto(
    Guid DocumentId,
    Guid DocumentChunkId,
    string FileName,
    double Score,
    bool IsOutdated);
