using MediatR;

namespace Normora.Modules.Conversations.Application.Conversations.Commands;

/// <summary>
/// Command to permanently remove a conversation thread belonging to the authenticated user.
/// </summary>
/// <param name="Id">The unique identifier of the target conversation.</param>
public record DeleteConversationCommand(Guid Id) : IRequest;

