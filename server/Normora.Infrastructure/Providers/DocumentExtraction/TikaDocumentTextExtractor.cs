using System.Net.Http.Headers;
using Normora.Modules.Documents.Persistence;

namespace Normora.Infrastructure.Providers.DocumentExtraction;

/// <summary>
/// An implementation of <see cref="IDocumentTextExtractor"/> that delegates extraction to an Apache Tika service.
/// </summary>
public sealed class TikaDocumentTextExtractor(HttpClient httpClient) : IDocumentTextExtractor
{
    public async Task<string> ExtractAsync(
        Stream documentStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        // Apache Tika REST Streaming Pipeline:
        // Streams the raw document bytes directly to the Apache Tika server via HTTP PUT /tika.
        // Using StreamContent avoids buffering large PDFs or DOCX files entirely into RAM.
        using var content = new StreamContent(documentStream);
        content.Headers.ContentType = new MediaTypeHeaderValue(GetContentType(fileName));

        // Tika Text Extraction:
        // Tika parses binary structures, OCRs scanned text, extracts metadata, and returns clean plain text.
        using var response = await httpClient.PutAsync("tika", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private static string GetContentType(string fileName)
    {
        return Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };
    }
}