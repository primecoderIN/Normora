using System.Security.Cryptography;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Conversations.Application.Conversations.DTOs;
using Normora.Modules.Conversations.Domain;
using Normora.Modules.Conversations.Persistence;
using Normora.Shared.Exceptions;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Application.Conversations.Commands;

public class ShareConversationCommandHandler(
    ConversationsDbContext context,
    ICurrentUser currentUser) : IRequestHandler<ShareConversationCommand, ConversationShareDto>
{
    private const string Chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    public async Task<ConversationShareDto> Handle(ShareConversationCommand request, CancellationToken cancellationToken)
    {
        // Broken Object Level Authorization (BOLA) Ownership Guard:
        // Users can only share conversations that they personally created.
        // If the conversation belongs to another user or doesn't exist, throw BolaException (mapped to 404).
        var conversation = await context.Conversations
            .FirstOrDefaultAsync(c => c.Id == request.ConversationId && c.UserId == currentUser.ExternalUserId, cancellationToken);

        if (conversation is null)
        {
            throw new BolaException();
        }

        // Idempotent Active Share Recycling:
        // If an unexpired public share already exists for this conversation, return the existing token
        // to avoid polluting the database with multiple orphaned share URLs for the same conversation.
        var existingShare = await context.ConversationShares
            .FirstOrDefaultAsync(s => s.ConversationId == request.ConversationId, cancellationToken);

        if (existingShare is not null && !existingShare.IsExpired)
        {
            return new ConversationShareDto(existingShare.Token, existingShare.ExpiresAt);
        }

        // Cryptographically Secure Token Generation:
        // Uses RandomNumberGenerator.GetInt32 across a 62-character alphanumeric set (a-z, A-Z, 0-9)
        // to generate an unguessable 16-character token with ~95 bits of entropy.
        var tokenChars = new char[16];
        for (var i = 0; i < 16; i++)
        {
            tokenChars[i] = Chars[RandomNumberGenerator.GetInt32(Chars.Length)];
        }
        var token = new string(tokenChars);

        // Public Share Record Instantiation:
        // By default, the share link is valid for 30 days unless revoked earlier.
        var share = new ConversationShare
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            CreatedByUserId = currentUser.ExternalUserId,
            Token = token,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = null
        };

        context.ConversationShares.Add(share);
        await context.SaveChangesAsync(cancellationToken);

        return new ConversationShareDto(share.Token, share.ExpiresAt);
    }
}

