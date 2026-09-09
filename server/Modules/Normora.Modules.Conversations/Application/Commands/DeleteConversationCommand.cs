using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Conversations.Persistence;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Application.Commands;

public record DeleteConversationCommand(Guid Id) : IRequest;

public class DeleteConversationCommandHandler(
    ConversationsDbContext context,
    ICurrentUser currentUser) : IRequestHandler<DeleteConversationCommand>
{
    public async Task Handle(DeleteConversationCommand request, CancellationToken cancellationToken)
    {
        var conversation = await context.Conversations
            .Where(c => c.Id == request.Id && c.UserId == currentUser.KeycloakUserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (conversation is null)
        {
            // We ignore not found for delete, or throw an exception depending on the project's standards.
            // Let's just return to make it idempotent.
            return;
        }

        context.Conversations.Remove(conversation);
        await context.SaveChangesAsync(cancellationToken);
    }
}
