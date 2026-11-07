using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Conversations.Domain;
using Normora.Modules.Conversations.Persistence;
using Normora.Shared.Constants;
using Normora.Shared.Exceptions;
using Normora.Shared.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace Normora.Modules.Conversations.Application.Conversations.Commands;

/// <summary>Saves feedback only for an assistant message visible in the caller's tenant.</summary>
internal sealed class SubmitFeedbackCommandHandler(
    ConversationsDbContext dbContext,
    ICurrentUser currentUser) : IRequestHandler<SubmitFeedbackCommand>
{
    public async Task Handle(SubmitFeedbackCommand request, CancellationToken cancellationToken)
    {
        // Broken Object Level Authorization (BOLA) Validation:
        // Ensures the target message exists within the specified conversation thread and caller's tenant.
        // If not found, throw BolaException (mapped to 404).
        var message = await dbContext.Messages
            .FirstOrDefaultAsync(m => m.Id == request.MessageId && m.ConversationId == request.ConversationId, cancellationToken)
            ?? throw new BolaException();

        // Invariant: Feedback Permitted Only on Assistant Responses
        // Users cannot submit evaluation feedback on their own prompt inputs or system instructions.
        if (message.Role != MessageRole.Assistant)
        {
            throw new InvalidOperationException(Normora.Shared.Constants.ApiMessages.FeedbackOnlyForAssistant);
        }

        // Idempotent Feedback Upsert Workflow:
        // If the user already provided feedback on this message, update their existing rating (thumbs up/down) and comment.
        // Otherwise, insert a new MessageFeedback record stamped with their external user ID.
        var feedback = await dbContext.MessageFeedbacks
            .FirstOrDefaultAsync(f => f.MessageId == request.MessageId && f.UserId == currentUser.ExternalUserId, cancellationToken);

        if (feedback is not null)
        {
            feedback.Rating = request.Rating;
            feedback.Comment = request.Comment;
        }
        else
        {
            feedback = new MessageFeedback
            {
                UserId = currentUser.ExternalUserId,
                MessageId = request.MessageId,
                Rating = request.Rating,
                Comment = request.Comment
            };
            dbContext.MessageFeedbacks.Add(feedback);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
