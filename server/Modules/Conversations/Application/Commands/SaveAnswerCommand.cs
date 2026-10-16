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

        // Verify the message exists and belongs to the current user (BOLA protection)
        var message = await context.Messages
            .AsNoTracking()
            .Include(m => m.Conversation)
            .FirstOrDefaultAsync(
                m => m.Id == request.MessageId &&
                     m.Conversation.UserId == currentUser.KeycloakUserId,
                cancellationToken)
            ?? throw new Normora.Shared.Exceptions.BolaException();

        if (message.Role != MessageRole.Assistant)
            throw new InvalidOperationException(Normora.Shared.Constants.ApiMessages.SaveOnlyAssistant);

        // BUG-1: Guard against null TenantId before using the null-forgiveness operator
        if (!tenantContext.TenantId.HasValue)
            throw new InvalidOperationException(Normora.Shared.Constants.ApiMessages.TenantContextMissing);

        var savedAnswer = new SavedAnswer
        {
            Id = Guid.NewGuid(),
            TenantId = tenantContext.TenantId.Value,
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
