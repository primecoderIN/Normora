namespace Normora.Modules.Documents.Persistence;

public interface IDocumentTextExtractor
{
    Task<string> ExtractAsync(Stream documentStream, string fileName, CancellationToken cancellationToken = default);
}