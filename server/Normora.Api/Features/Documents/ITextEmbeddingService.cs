namespace Normora.Api.Features.Documents;

public interface ITextEmbeddingService
{
    bool IsConfigured { get; }
    Task<float[]> CreateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
}