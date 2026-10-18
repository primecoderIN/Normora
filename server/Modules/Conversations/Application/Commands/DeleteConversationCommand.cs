using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Conversations.Persistence;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Application.Commands;

/// <summary>
/// Command to permanently remove a conversation thread belonging to the authenticated user.
/// </summary>
/// <param name="Id">The unique identifier of the target conversation.</param>
public record DeleteConversationCommand(Guid Id) : IRequest;

/// <summary>
/// Handles conversation deletion with strict ownership validation to prevent BOLA attacks.
/// </summary>
public class DeleteConversationCommandHandler(
    ConversationsDbContext context,
    ICurrentUser currentUser) : IRequestHandler<DeleteConversationCommand>
{
    public async Task Handle(DeleteConversationCommand request, CancellationToken cancellationToken)
    {
        // Enforce user ownership: users can only delete their own conversations
        var conversation = await context.Conversations
            .Where(c => c.Id == request.Id && c.UserId == currentUser.KeycloakUserId)
            .FirstOrDefaultAsync(cancellationToken);

        // Throw BolaException mapped to 404 Not Found to prevent resource enumeration
        if (conversation is null)
        {
            throw new Normora.Shared.Exceptions.BolaException();
        }

        context.Conversations.Remove(conversation);
        await context.SaveChangesAsync(cancellationToken);
    }
}

