namespace Normora.Modules.Conversations.Application.Conversations.DTOs;

public record SharedConversationDto(
    Guid Id,
    string Title,
    string? Summary,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset LastMessageAt,
    IReadOnlyList<MessageDto> Messages,
    string? SharedByName);

