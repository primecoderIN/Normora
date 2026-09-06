namespace Normora.Modules.Documents.Persistence;

/// <summary>
/// Defines the contract for extracting raw text from various document formats (PDF, DOCX, etc.).
/// </summary>
public interface IDocumentTextExtractor
{
    /// <summary>
    /// Extracts text from the provided document stream.
    /// </summary>
    /// <param name="documentStream">The stream of the document file.</param>
    /// <param name="fileName">The name of the file, used to determine format.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The raw extracted text.</returns>
    Task<string> ExtractAsync(Stream documentStream, string fileName, CancellationToken cancellationToken = default);
}