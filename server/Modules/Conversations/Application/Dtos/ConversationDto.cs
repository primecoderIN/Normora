namespace Normora.Modules.Conversations.Application.Dtos;

/// <summary>
/// Data transfer object representing conversation thread header metadata.
/// </summary>
/// <param name="Id">Unique conversation identifier.</param>
/// <param name="Title">Thread title.</param>
/// <param name="Summary">Rolling conversation summary if generated.</param>
/// <param name="CreatedAt">Thread creation timestamp.</param>
/// <param name="UpdatedAt">Thread last updated timestamp.</param>
/// <param name="LastMessageAt">Timestamp of the latest message in thread.</param>
public record ConversationDto(
    Guid Id,
    string Title,
    string? Summary,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset LastMessageAt);

