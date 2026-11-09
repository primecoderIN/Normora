using Hangfire;
using Normora.Modules.Documents.Application.Jobs;

namespace Normora.Infrastructure.BackgroundJobs;

public sealed class HangfirePurgeDeletedDocumentsJob(PurgeDeletedDocumentsJob purgeDeletedDocumentsJob)
{
    [AutomaticRetry(Attempts = 3)]
    public Task ExecuteAsync(CancellationToken cancellationToken) =>
        purgeDeletedDocumentsJob.ExecuteAsync(cancellationToken);
}
