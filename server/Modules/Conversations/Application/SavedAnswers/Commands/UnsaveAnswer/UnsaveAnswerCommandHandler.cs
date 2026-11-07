using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Conversations.Persistence;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Application.SavedAnswers.Commands;

public class UnsaveAnswerCommandHandler(
    ConversationsDbContext context,
    ICurrentUser currentUser) : IRequestHandler<UnsaveAnswerCommand>
{
    public async Task Handle(UnsaveAnswerCommand request, CancellationToken cancellationToken)
    {
        var saved = await context.SavedAnswers
            .FirstOrDefaultAsync(
                s => s.MessageId == request.MessageId && s.UserId == currentUser.ExternalUserId,
                cancellationToken);

        if (saved is not null)
        {
            context.SavedAnswers.Remove(saved);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}

