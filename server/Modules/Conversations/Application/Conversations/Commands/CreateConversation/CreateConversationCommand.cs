using MediatR;
using Normora.Modules.Conversations.Application.Conversations.DTOs;

namespace Normora.Modules.Conversations.Application.Conversations.Commands;

/// <summary>
/// Command to initialize a new conversation thread for the current authenticated user and tenant.
/// </summary>
public record CreateConversationCommand() : IRequest<ConversationDto>;

