using Hangfire;
using Normora.Modules.Documents.Application.Services;

namespace Normora.Infrastructure.BackgroundJobs;

public sealed class HangfireDocumentProcessingWakeup(IBackgroundJobClient backgroundJobClient) : IDocumentProcessingWakeup
{
    public Task SignalAsync(CancellationToken cancellationToken = default)
    {
        backgroundJobClient.Enqueue<HangfireDocumentProcessingOutboxDispatcher>(
            dispatcher => dispatcher.DispatchPendingAsync(CancellationToken.None));

        return Task.CompletedTask;
    }
}
