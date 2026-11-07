using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Conversations.Persistence;
using Normora.Shared.Exceptions;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Application.Conversations.Commands;

/// <summary>
/// Handles conversation deletion with strict ownership validation to prevent BOLA attacks.
/// </summary>
public class DeleteConversationCommandHandler(
    ConversationsDbContext context,
    ICurrentUser currentUser) : IRequestHandler<DeleteConversationCommand>
{
    public async Task Handle(DeleteConversationCommand request, CancellationToken cancellationToken)
    {
        // Broken Object Level Authorization (BOLA) Ownership Guard:
        // A user is strictly restricted to deleting conversations they personally authored (UserId == currentUser.ExternalUserId).
        // If the ID does not exist or belongs to another employee or workspace, we throw a BolaException.
        // This is mapped globally to a generic 404 Not Found to prevent malicious thread enumeration.
        var conversation = await context.Conversations
            .Where(c => c.Id == request.Id && c.UserId == currentUser.ExternalUserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (conversation is null)
        {
            throw new BolaException();
        }

        // Relational Cascade Cleanup:
        // Removing the conversation entity cascades through EF Core configuration to purge:
        // 1. All associated messages (MessageRole.User, Assistant, System).
        // 2. All message citations (MessageCitation).
        // 3. Any active public share links (ConversationShare).
        context.Conversations.Remove(conversation);
        await context.SaveChangesAsync(cancellationToken);
    }
}

