namespace Normora.Modules.Conversations.Application.Conversations.DTOs;

/// <summary>
/// Data transfer object representing a created shareable token for an employee conversation thread.
/// </summary>
public record ConversationShareDto(
    // High-entropy token used in public share URLs (/share/{token}).
    string Token,

    // Optional expiration timestamp after which the link becomes inactive.
    DateTimeOffset? ExpiresAt
);

