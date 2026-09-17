namespace Normora.Modules.Conversations.Application.Dtos;

/// <summary>
/// A bookmark DTO returned to the client for the Saved Answers page.
/// Contains the message content and its citations so the page is self-contained.
/// </summary>
public record SavedAnswerDto(
    Guid Id,
    Guid MessageId,
    Guid ConversationId,
    string Content,
    IReadOnlyList<CitationDto> Citations,
    DateTimeOffset SavedAt);

public record CitationDto(
    Guid DocumentId,
    string FileName,
    double Score);
