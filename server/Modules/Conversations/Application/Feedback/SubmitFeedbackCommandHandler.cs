using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Conversations.Domain;
using Normora.Modules.Conversations.Persistence;
using Normora.Shared.Constants;
using Normora.Shared.Exceptions;
using Normora.Shared.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace Normora.Modules.Conversations.Application.Feedback;

internal sealed class SubmitFeedbackCommandHandler(
    ConversationsDbContext dbContext,
    ICurrentUser currentUser) : IRequestHandler<SubmitFeedbackCommand>
{
    public async Task Handle(SubmitFeedbackCommand request, CancellationToken cancellationToken)
    {
        var message = await dbContext.Messages
            .FirstOrDefaultAsync(m => m.Id == request.MessageId && m.ConversationId == request.ConversationId, cancellationToken)
            ?? throw new BolaException();

        if (message.Role != MessageRole.Assistant)
        {
            throw new InvalidOperationException(Normora.Shared.Constants.ApiMessages.FeedbackOnlyForAssistant);
        }

        var feedback = await dbContext.MessageFeedbacks
            .FirstOrDefaultAsync(f => f.MessageId == request.MessageId && f.UserId == currentUser.KeycloakUserId, cancellationToken);

        if (feedback is not null)
        {
            feedback.Rating = request.Rating;
            feedback.Comment = request.Comment;
            // Optionally update CreatedAt or add an UpdatedAt field, but plan says immutable or updated. We will update.
        }
        else
        {
            feedback = new MessageFeedback
            {
                UserId = currentUser.KeycloakUserId,
                MessageId = request.MessageId,
                Rating = request.Rating,
                Comment = request.Comment
            };
            dbContext.MessageFeedbacks.Add(feedback);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
