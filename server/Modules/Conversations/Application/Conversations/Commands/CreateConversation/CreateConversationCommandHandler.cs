using MediatR;
using Normora.Modules.Conversations.Application.Conversations.DTOs;
using Normora.Modules.Conversations.Domain;
using Normora.Modules.Conversations.Persistence;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Application.Conversations.Commands;

/// <summary>
/// Handles conversation thread creation with default title and zeroed summary state.
/// </summary>
public class CreateConversationCommandHandler(
    ConversationsDbContext context,
    ICurrentUser currentUser,
    ITenantContext tenantContext) : IRequestHandler<CreateConversationCommand, ConversationDto>
{
    public async Task<ConversationDto> Handle(CreateConversationCommand request, CancellationToken cancellationToken)
    {
        // Thread Initialization & Multi-Tenant Scoping:
        // Creates a new conversation thread bound to:
        // 1. ExternalUserId: The OIDC subject identity of the caller (ensuring employees only see their own chat history).
        // 2. TenantId: The active workspace context (ensuring conversation data never crosses organizational boundaries).
        // 3. SummaryVersion = 0: Initializes the rolling summary version counter for long-running context summarization.
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            UserId = currentUser.ExternalUserId,
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

