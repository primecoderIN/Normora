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
            throw new Normora.Shared.Exceptions.BolaException();
        }

        context.Conversations.Remove(conversation);
        await context.SaveChangesAsync(cancellationToken);
    }
}
