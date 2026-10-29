using System.Security.Cryptography;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Conversations.Domain;
using Normora.Modules.Conversations.Persistence;
using Normora.Shared.Exceptions;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Application.Commands;

/// <summary>Creates or reuses a secure public share token for a conversation owned by the caller.</summary>
public record ShareConversationCommand(Guid ConversationId) : IRequest<ConversationShareDto>;

public record ConversationShareDto(string Token, DateTimeOffset? ExpiresAt);

public class ShareConversationCommandHandler(
    ConversationsDbContext context,
    ICurrentUser currentUser) : IRequestHandler<ShareConversationCommand, ConversationShareDto>
{
    private const string Chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    public async Task<ConversationShareDto> Handle(ShareConversationCommand request, CancellationToken cancellationToken)
    {
        // 1. Verify caller owns the conversation
        var conversation = await context.Conversations
            .FirstOrDefaultAsync(c => c.Id == request.ConversationId && c.UserId == currentUser.KeycloakUserId, cancellationToken);

        if (conversation is null)
        {
            throw new BolaException();
        }

        // 2. Check for an existing valid share to ensure idempotency
        var existingShare = await context.ConversationShares
            .FirstOrDefaultAsync(s => s.ConversationId == request.ConversationId, cancellationToken);

        if (existingShare is not null && (existingShare.ExpiresAt is null || existingShare.ExpiresAt > DateTimeOffset.UtcNow))
        {
            return new ConversationShareDto(existingShare.Token, existingShare.ExpiresAt);
        }

        // 3. Generate a cryptographically secure 16-character alphanumeric slug
        var tokenChars = new char[16];
        for (var i = 0; i < 16; i++)
        {
            tokenChars[i] = Chars[RandomNumberGenerator.GetInt32(Chars.Length)];
        }
        var token = new string(tokenChars);

        // 4. Create and persist the share record
        var share = new ConversationShare
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            CreatedByUserId = currentUser.KeycloakUserId,
            Token = token,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = null
        };

        context.ConversationShares.Add(share);
        await context.SaveChangesAsync(cancellationToken);

        return new ConversationShareDto(share.Token, share.ExpiresAt);
    }
}
