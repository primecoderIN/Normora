namespace Normora.Modules.Conversations.Application.Dtos;

public record ConversationDto(
    Guid Id,
    string Title,
    string? Summary,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset LastMessageAt);
