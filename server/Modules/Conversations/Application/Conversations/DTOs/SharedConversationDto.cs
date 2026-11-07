namespace Normora.Modules.Conversations.Application.Conversations.DTOs;

/// <summary>
/// Data transfer object delivering a read-only snapshot of a shared conversation thread to external or non-authenticated viewers.
/// </summary>
public record SharedConversationDto(
    Guid Id,
    string Title,
    string? Summary,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset LastMessageAt,
    IReadOnlyList<MessageDto> Messages,
    string? SharedByName
);

