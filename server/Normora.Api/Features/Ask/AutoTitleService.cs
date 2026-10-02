using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.Metrics;
using Normora.Modules.Conversations.Persistence;
using Normora.Shared.Constants;

namespace Normora.Api.Features.Ask;

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
                var db = scope.ServiceProvider.GetRequiredService<ConversationsDbContext>();
                var gen = scope.ServiceProvider.GetRequiredService<ITextGenerationService>();

                var title = await gen.GenerateTitleAsync(firstQuestion);
                if (string.IsNullOrWhiteSpace(title)) return;

                var conv = await db.Conversations
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(c => c.Id == conversationId);
                if (conv is null || conv.Title != ConversationConstants.DefaultTitle) return;

                conv.Title = title;
                conv.UpdatedAt = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync();

                metrics.AutoTitleOperations.Add(1, new KeyValuePair<string, object?>("status", "success"));
            }
            catch
            {
                // Titles are best-effort — never surface auto-title failures to the caller
                metrics.AutoTitleOperations.Add(1, new KeyValuePair<string, object?>("status", "failure"));
            }
        });
    }
}
