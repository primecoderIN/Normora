using MediatR;
using Normora.Modules.Conversations.Application.Dtos;
using Normora.Modules.Conversations.Domain;
using Normora.Modules.Conversations.Persistence;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Application.Commands;

public record CreateConversationCommand() : IRequest<ConversationDto>;

public class CreateConversationCommandHandler(
    ConversationsDbContext context,
    ICurrentUser currentUser,
    ITenantContext tenantContext) : IRequestHandler<CreateConversationCommand, ConversationDto>
{
    public async Task<ConversationDto> Handle(CreateConversationCommand request, CancellationToken cancellationToken)
    {
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            UserId = currentUser.KeycloakUserId,
            TenantId = tenantContext.TenantId!.Value,
            Title = "New conversation",
            SummaryVersion = 0,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            LastMessageAt = DateTimeOffset.UtcNow
        };

        context.Conversations.Add(conversation);
        await context.SaveChangesAsync(cancellationToken);

        return new ConversationDto(
            conversation.Id,
            conversation.Title,
            conversation.Summary,
            conversation.CreatedAt,
            conversation.UpdatedAt,
            conversation.LastMessageAt);
    }
}
