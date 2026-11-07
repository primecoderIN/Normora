namespace Normora.Modules.Conversations.Application.SavedAnswers.DTOs;

/// <summary>
/// Data transfer object representing a bookmarked AI answer.
/// Contains the markdown message body and source document citations for rendering in the employee's saved answers portal.
/// </summary>
public record SavedAnswerDto(
    // Unique identifier of the bookmark record.
    Guid Id,

    // Target message identifier.
    Guid MessageId,

    // Originating conversation thread identifier.
    Guid ConversationId,

    // Markdown text content of the assistant response.
    string Content,

    // Citation references grounding this answer in tenant policy documents.
    IReadOnlyList<CitationDto> Citations,

    // UTC timestamp when the answer was saved.
    DateTimeOffset SavedAt
);

/// <summary>
/// Citation reference metadata attached to a saved answer.
/// </summary>
public record CitationDto(
    // Unique identifier of the cited document.
    Guid DocumentId,

    // File name of the source document (e.g. "Employee-Handbook.pdf").
    string FileName,

    // Vector relevance/similarity score.
    double Score,

    // Indicates whether the underlying document version has been superseded or replaced.
    bool IsOutdated
);
