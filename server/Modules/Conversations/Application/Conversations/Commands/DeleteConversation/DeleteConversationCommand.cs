using MediatR;

namespace Normora.Modules.Conversations.Application.Conversations.Commands;

/// <summary>
/// Command to permanently remove a conversation thread and its associated message turns.
/// Handled by <see cref="DeleteConversationCommandHandler"/>.
/// </summary>
/// <remarks>
/// Authorization & Cascading:
/// - Verifies that the conversation is owned by the authenticated caller within the ambient TenantId (BOLA protection).
/// - Cascades deletion to all child <see cref="Domain.Message"/>, <see cref="Domain.MessageCitation"/>, and <see cref="Domain.SavedAnswer"/> entities.
/// </remarks>
/// <param name="Id">The unique identifier of the target conversation thread.</param>
public record DeleteConversationCommand(Guid Id) : IRequest;

