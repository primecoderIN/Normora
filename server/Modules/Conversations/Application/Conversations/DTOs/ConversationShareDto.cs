namespace Normora.Modules.Conversations.Application.Conversations.DTOs;

public record ConversationShareDto(string Token, DateTimeOffset? ExpiresAt);

