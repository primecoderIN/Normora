using MediatR;
using Normora.Modules.Conversations.Application.Conversations.DTOs;

namespace Normora.Modules.Conversations.Application.Conversations.Commands;

/// <summary>
/// Command to initialize a new conversation thread for the current authenticated user and ambient tenant.
/// Handled by <see cref="CreateConversationCommandHandler"/>.
/// </summary>
/// <remarks>
/// Lifecycle & Title Invariants:
/// - Assigns the initial placeholder title from <see cref="Shared.Constants.ConversationConstants.DefaultTitle"/> ("New Conversation").
/// - Binds ownership to the caller's UserId and ambient TenantId (enforcing multi-tenant BOLA isolation).
/// - Asynchronously auto-titled upon the first question/answer turn via <see cref="Services.IAutoTitleService"/>.
/// </remarks>
public record CreateConversationCommand() : IRequest<ConversationDto>;

