using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Conversations.Domain;
using Normora.Modules.Conversations.Persistence;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Application.Commands;

/// <summary>
/// Saves an assistant message as a bookmark for the current user.
/// Idempotent — silently succeeds if the message is already saved.
/// </summary>
public record SaveAnswerCommand(Guid MessageId) : IRequest<Guid>;

public class SaveAnswerCommandHandler(
    ConversationsDbContext context,
    ICurrentUser currentUser,
    ITenantContext tenantContext) : IRequestHandler<SaveAnswerCommand, Guid>
{
    public async Task<Guid> Handle(SaveAnswerCommand request, CancellationToken cancellationToken)
    {
        // Check if already saved by this user (idempotency)
        var existing = await context.SavedAnswers
            .AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.UserId == currentUser.KeycloakUserId && s.MessageId == request.MessageId,
                cancellationToken);

        if (existing is not null)
            return existing.Id;

        // Verify the message exists and belongs to this tenant/user
        var message = await context.Messages
            .AsNoTracking()
            .Include(m => m.Conversation)
            .FirstOrDefaultAsync(m => m.Id == request.MessageId, cancellationToken)
            ?? throw new InvalidOperationException("Message not found.");

        if (message.Role != MessageRole.Assistant)
            throw new InvalidOperationException("Only assistant messages can be saved.");

        var savedAnswer = new SavedAnswer
        {
            Id = Guid.NewGuid(),
            TenantId = tenantContext.TenantId!.Value,
            UserId = currentUser.KeycloakUserId,
            MessageId = request.MessageId,
            ConversationId = message.ConversationId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        context.SavedAnswers.Add(savedAnswer);
        await context.SaveChangesAsync(cancellationToken);

        return savedAnswer.Id;
    }
}
