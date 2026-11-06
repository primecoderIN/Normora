using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Conversations.Persistence;
using Normora.Shared.Constants;

namespace Normora.Modules.Conversations.Application.Conversations.Commands;

public sealed class UpdateConversationTitleCommandHandler(ConversationsDbContext db)
    : IRequestHandler<UpdateConversationTitleCommand, bool>
{
    public async Task<bool> Handle(UpdateConversationTitleCommand request, CancellationToken cancellationToken)
    {
        var conv = await db.Conversations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == request.ConversationId, cancellationToken);

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

