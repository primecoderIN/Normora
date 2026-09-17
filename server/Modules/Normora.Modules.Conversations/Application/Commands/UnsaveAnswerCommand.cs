using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Conversations.Persistence;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Application.Commands;

/// <summary>
/// Removes a previously saved answer for the current user.
/// Idempotent — silently succeeds if the saved answer does not exist.
/// </summary>
public record UnsaveAnswerCommand(Guid MessageId) : IRequest;

public class UnsaveAnswerCommandHandler(
    ConversationsDbContext context,
    ICurrentUser currentUser) : IRequestHandler<UnsaveAnswerCommand>
{
    public async Task Handle(UnsaveAnswerCommand request, CancellationToken cancellationToken)
    {
        var saved = await context.SavedAnswers
            .FirstOrDefaultAsync(
                s => s.UserId == currentUser.KeycloakUserId && s.MessageId == request.MessageId,
                cancellationToken);

        if (saved is null) return;

        context.SavedAnswers.Remove(saved);
        await context.SaveChangesAsync(cancellationToken);
    }
}
