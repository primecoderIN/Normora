using MediatR;
using Normora.Modules.Conversations.Application.Conversations.DTOs;

namespace Normora.Modules.Conversations.Application.Conversations.Commands;

/// <summary>
/// Command to create or reuse a secure high-entropy share token for a conversation thread.
/// Handled by <see cref="ShareConversationCommandHandler"/>.
/// </summary>
/// <remarks>
/// Sharing Security:
/// - Verifies thread ownership within the caller's ambient TenantId (BOLA protection).
/// - Idempotent: reuses an existing active share token if already generated for this conversation.
/// - Returns a <see cref="ConversationShareDto"/> containing the share token and expiration timestamp.
/// </remarks>
/// <param name="ConversationId">The target conversation thread identifier.</param>
public record ShareConversationCommand(Guid ConversationId) : IRequest<ConversationShareDto>;

