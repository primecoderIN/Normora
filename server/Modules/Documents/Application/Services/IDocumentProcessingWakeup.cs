namespace Normora.Modules.Documents.Application.Services;

public interface IDocumentProcessingWakeup
{
    Task SignalAsync(CancellationToken cancellationToken = default);
}
