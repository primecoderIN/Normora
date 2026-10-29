using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Normora.Modules.Conversations.Application.Commands;

namespace Normora.Modules.Conversations.Application.Services;

public sealed class AutoTitleService(
    IServiceScopeFactory scopeFactory,
    ConversationMetrics metrics) : IAutoTitleService
{
    public void AutoTitleConversationAsync(Guid conversationId, string firstQuestion)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                // Own scope — request scope may be disposed by the time this runs
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
            catch
            {
                // Titles are best-effort — never surface auto-title failures to the caller
                metrics.AutoTitleOperations.Add(1, new KeyValuePair<string, object?>("status", "failure"));
            }
        });
    }
}
