using System.Runtime.CompilerServices;
using System.Text.Json;
using MediatR;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Normora.Modules.Conversations.Application.Conversations.Commands;
using Normora.Modules.Conversations.Application.Conversations.DTOs;
using Normora.Shared.Constants;
using Normora.Shared.Interfaces;

namespace Normora.Infrastructure.Agents;

/// <summary>
/// Session state container for <see cref="NormoraWorkplaceAgent"/>.
/// </summary>
public sealed class NormoraAgentSession : AgentSession
{
    public NormoraAgentSession() : base() { }
    public NormoraAgentSession(AgentSessionStateBag stateBag) : base(stateBag) { }
}

/// <summary>
/// Microsoft Agent Framework agent implementation for the Normora Workplace Intelligence Copilot.
/// Bridges CopilotKit / AG-UI client interactions to Normora's conversational RAG pipeline,
/// enforcing tenant isolation, multi-tenant BOLA boundaries, and streaming token generation.
/// </summary>
public sealed class NormoraWorkplaceAgent(
    ISender mediator,
    ITenantContext tenantContext,
    ILogger<NormoraWorkplaceAgent> logger) : AIAgent
{
    public override string? Name => "NormoraWorkplaceAgent";
    public override string? Description => "Normora AI Workplace Intelligence Copilot";

    protected override ValueTask<AgentSession> CreateSessionCoreAsync(CancellationToken cancellationToken)
        => ValueTask.FromResult<AgentSession>(new NormoraAgentSession());

    protected override ValueTask<JsonElement> SerializeSessionCoreAsync(
        AgentSession session,
        JsonSerializerOptions? options,
        CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(session.StateBag.Serialize());
    }

    protected override ValueTask<AgentSession> DeserializeSessionCoreAsync(
        JsonElement element,
        JsonSerializerOptions? options,
        CancellationToken cancellationToken)
    {
        var bag = AgentSessionStateBag.Deserialize(element);
        return ValueTask.FromResult<AgentSession>(new NormoraAgentSession(bag));
    }

    protected override async Task<AgentResponse> RunCoreAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session,
        AgentRunOptions? options,
        CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue)
        {
            logger.LogWarning("Agent invocation rejected: TenantContext is missing.");
            throw new InvalidOperationException(ApiMessages.TenantContextMissing);
        }

        var messageList = messages as IList<ChatMessage> ?? messages.ToList();
        var userMessage = messageList.LastOrDefault(m => m.Role == ChatRole.User);
        var question = userMessage?.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(question))
        {
            return new AgentResponse(new ChatMessage(
                ChatRole.Assistant,
                "Hello! I am Normora Copilot, your AI workplace assistant. How can I help you today?"));
        }

        var conversationId = await ResolveConversationIdAsync(session, cancellationToken);
        var result = await mediator.Send(new AskConversationCommand(conversationId, question), cancellationToken);

        return new AgentResponse(new ChatMessage(ChatRole.Assistant, result.Answer));
    }

    protected override async IAsyncEnumerable<AgentResponseUpdate> RunCoreStreamingAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session,
        AgentRunOptions? options,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue)
        {
            logger.LogWarning("Agent streaming rejected: TenantContext is missing.");
            throw new InvalidOperationException(ApiMessages.TenantContextMissing);
        }

        var messageList = messages as IList<ChatMessage> ?? messages.ToList();
        var userMessage = messageList.LastOrDefault(m => m.Role == ChatRole.User);
        var question = userMessage?.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(question))
        {
            yield return new AgentResponseUpdate(
                ChatRole.Assistant,
                "Hello! I am Normora Copilot, your AI workplace assistant. How can I help you today?");
            yield break;
        }

        var conversationId = await ResolveConversationIdAsync(session, cancellationToken);
        var stream = mediator.CreateStream(new AskConversationStreamCommand(conversationId, question), cancellationToken);

        await foreach (var evt in stream.WithCancellation(cancellationToken))
        {
            if (evt is TextChunkEvent textChunk && !string.IsNullOrEmpty(textChunk.Text))
            {
                yield return new AgentResponseUpdate(ChatRole.Assistant, textChunk.Text);
            }
        }
    }

    private async Task<Guid> ResolveConversationIdAsync(AgentSession? session, CancellationToken cancellationToken)
    {
        if (session != null &&
            session.StateBag.TryGetValue<string>("ConversationId", out var savedConvIdStr) &&
            Guid.TryParse(savedConvIdStr, out var existingConvId))
        {
            return existingConvId;
        }

        var created = await mediator.Send(new CreateConversationCommand(), cancellationToken);
        session?.StateBag.SetValue("ConversationId", created.Id.ToString());
        return created.Id;
    }
}
