using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Conversations.Persistence;
using Normora.Shared.Constants;

namespace Normora.Modules.Conversations.Application.Conversations.Commands;

/// <summary>
/// Command handler that applies auto-generated conversation titles safely.
/// Bypasses ambient query filters (.IgnoreQueryFilters()) because it runs inside an asynchronous background task.
/// </summary>
public sealed class UpdateConversationTitleCommandHandler(ConversationsDbContext db)
    : IRequestHandler<UpdateConversationTitleCommand, bool>
{
    public async Task<bool> Handle(UpdateConversationTitleCommand request, CancellationToken cancellationToken)
    {
        // Ignore ambient query filters: auto-title runs as a detached background task where ITenantContext may be ambiently unbound.
        var conv = await db.Conversations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == request.ConversationId, cancellationToken);

        // Guard: do not overwrite title if the conversation does not exist or user has already renamed it.
        if (conv is null || conv.Title != ConversationConstants.DefaultTitle)
        {
            return false;
        }

        conv.Title = request.Title;
        conv.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return true;
    }
}

