using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Normora.Modules.Conversations.Application.Conversations.Commands;

namespace Normora.Modules.Conversations.Application.Services;

/// <summary>Generates and persists conversation titles in a separate scope off the response path.</summary>
internal sealed class AutoTitleService(
    IServiceScopeFactory scopeFactory,
    ConversationMetrics metrics,
    ILogger<AutoTitleService> logger,
    IHostApplicationLifetime? hostLifetime = null) : IAutoTitleService
{
    public void AutoTitleConversationAsync(Guid conversationId, string firstQuestion)
    {
        var stoppingToken = hostLifetime?.ApplicationStopping ?? CancellationToken.None;
        if (stoppingToken.IsCancellationRequested) return;

        // Background title generation tied to the application host lifetime:
        // Runs off the HTTP request path so the user's initial streaming response is not blocked,
        // while respecting graceful shutdown via IHostApplicationLifetime.
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

                var title = await gen.GenerateTitleAsync(firstQuestion, stoppingToken);
                if (string.IsNullOrWhiteSpace(title) || stoppingToken.IsCancellationRequested) return;

                var updated = await mediator.Send(new UpdateConversationTitleCommand(conversationId, title), stoppingToken);
                if (updated)
                {
                    metrics.AutoTitleOperations.Add(1, new KeyValuePair<string, object?>("status", "success"));
                }
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Auto-titling for conversation {ConversationId} canceled during application shutdown.", conversationId);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to auto-title conversation {ConversationId}", conversationId);
                metrics.AutoTitleOperations.Add(1, new KeyValuePair<string, object?>("status", "failure"));
            }
        }, stoppingToken);
    }
}
