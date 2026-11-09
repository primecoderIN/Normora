using Hangfire;
using Normora.Modules.Documents.Application.Jobs;

namespace Normora.Infrastructure.BackgroundJobs;

public sealed class HangfireDocumentProcessingJob(DocumentProcessingJob documentProcessingJob)
{
    [AutomaticRetry(Attempts = 3)]
    public Task ProcessAsync(
        Guid documentVersionId,
        Guid tenantId,
        CancellationToken cancellationToken = default) =>
        documentProcessingJob.ProcessAsync(documentVersionId, tenantId, cancellationToken);
}
