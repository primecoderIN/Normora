using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Conversations.Domain;
using Normora.Modules.Conversations.Persistence;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Application.SavedAnswers.Commands;

/// <summary>
/// Command handler that persists a bookmarked assistant answer for the authenticated employee.
/// Enforces idempotent bookmark creation and strict BOLA message ownership checks.
/// </summary>
public class SaveAnswerCommandHandler(
    ConversationsDbContext context,
    ICurrentUser currentUser,
    ITenantContext tenantContext) : IRequestHandler<SaveAnswerCommand, Guid>
{
    public async Task<Guid> Handle(SaveAnswerCommand request, CancellationToken cancellationToken)
    {
        // 1. Idempotency Check:
        // Returns the existing ID without failing if the user has already saved this response.
        var existing = await context.SavedAnswers
            .AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.UserId == currentUser.ExternalUserId && s.MessageId == request.MessageId,
                cancellationToken);

        if (existing is not null)
            return existing.Id;

        // 2. BOLA Authorization Verification:
        // Ensures the message exists and belongs to a thread created by the caller.
        var message = await context.Messages
            .AsNoTracking()
            .Include(m => m.Conversation)
            .FirstOrDefaultAsync(
                m => m.Id == request.MessageId &&
                     m.Conversation.UserId == currentUser.ExternalUserId,
                cancellationToken)
            ?? throw new Normora.Shared.Exceptions.BolaException();

        // 3. Role Validation:
        // Users may only bookmark assistant answers, not their own question prompts.
        if (message.Role != MessageRole.Assistant)
            throw new InvalidOperationException(Normora.Shared.Constants.ApiMessages.SaveOnlyAssistant);

        // Guard against missing tenant context before saving.
        if (!tenantContext.TenantId.HasValue)
            throw new InvalidOperationException(Normora.Shared.Constants.ApiMessages.TenantContextMissing);

        var savedAnswer = new SavedAnswer
        {
            Id = Guid.NewGuid(),
            TenantId = tenantContext.TenantId.Value,
            UserId = currentUser.ExternalUserId,
            MessageId = request.MessageId,
            ConversationId = message.ConversationId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        context.SavedAnswers.Add(savedAnswer);
        await context.SaveChangesAsync(cancellationToken);

        return savedAnswer.Id;
    }
}
