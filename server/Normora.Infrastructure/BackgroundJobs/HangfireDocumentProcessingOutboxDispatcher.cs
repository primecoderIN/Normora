using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Normora.Modules.Documents.Application.Jobs;
using Normora.Modules.Documents.Persistence;

namespace Normora.Infrastructure.BackgroundJobs;

public sealed class HangfireDocumentProcessingOutboxDispatcher(
    DocumentsDbContext context,
    IBackgroundJobClient backgroundJobClient,
    ILogger<HangfireDocumentProcessingOutboxDispatcher> logger)
{
    [DisableConcurrentExecution(timeoutInSeconds: 60)]
    [AutomaticRetry(Attempts = 3)]
    public async Task DispatchPendingAsync(CancellationToken cancellationToken = default)
    {
        var pendingMessages = await context.DocumentProcessingOutboxMessages
            .IgnoreQueryFilters()
            .OrderBy(message => message.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        foreach (var message in pendingMessages)
        {
            backgroundJobClient.Enqueue<HangfireDocumentProcessingJob>(job =>
                job.ProcessAsync(message.DocumentVersionId, message.TenantId));

            context.DocumentProcessingOutboxMessages.Remove(message);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Dispatched document processing for version {DocumentVersionId} in tenant {TenantId}.",
                message.DocumentVersionId,
                message.TenantId);
        }
    }
}
