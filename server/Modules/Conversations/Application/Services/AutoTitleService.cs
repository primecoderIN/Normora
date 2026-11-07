using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Normora.Modules.Conversations.Application.Conversations.Commands;

namespace Normora.Modules.Conversations.Application.Services;

/// <summary>Generates and persists conversation titles in a separate scope off the response path.</summary>
internal sealed class AutoTitleService(
    IServiceScopeFactory scopeFactory,
    ConversationMetrics metrics,
    ILogger<AutoTitleService> logger) : IAutoTitleService
{
    public void AutoTitleConversationAsync(Guid conversationId, string firstQuestion)
    {
        // Fire-and-forget background title generation:
        // Runs off the HTTP request path so the user's initial streaming response is not blocked by auto-titling LLM latency.
        _ = Task.Run(async () =>
        {
            try
            {
                // Explicit Service Scope:
                // Because this task executes asynchronously after the HTTP response has returned,
                // the originating request DI scope and DbContext will be disposed.
                // We create a dedicated IServiceScopeFactory scope to resolve fresh scoped dependencies.
                await using var scope = scopeFactory.CreateAsyncScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var gen = scope.ServiceProvider.GetRequiredService<ITextGenerationService>();

                var title = await gen.GenerateTitleAsync(firstQuestion);
                if (string.IsNullOrWhiteSpace(title)) return;

                var updated = await mediator.Send(new UpdateConversationTitleCommand(conversationId, title));
                if (updated)
                {
                    metrics.AutoTitleOperations.Add(1, new KeyValuePair<string, object?>("status", "success"));
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to auto-title conversation {ConversationId}", conversationId);
                metrics.AutoTitleOperations.Add(1, new KeyValuePair<string, object?>("status", "failure"));
            }
        });
    }
}
