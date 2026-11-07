using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Conversations.Persistence;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Application.SavedAnswers.Commands;

/// <summary>
/// Command handler that removes a bookmarked answer for the current employee.
/// Scoped strictly to the caller's ExternalUserId to prevent unauthorized deletion of other users' bookmarks.
/// </summary>
public class UnsaveAnswerCommandHandler(
    ConversationsDbContext context,
    ICurrentUser currentUser) : IRequestHandler<UnsaveAnswerCommand>
{
    public async Task Handle(UnsaveAnswerCommand request, CancellationToken cancellationToken)
    {
        // Enforce user-level isolation: only remove if the bookmark belongs to the caller.
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

