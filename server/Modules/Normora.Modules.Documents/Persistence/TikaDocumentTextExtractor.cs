using System.Net.Http.Headers;

namespace Normora.Modules.Documents.Persistence;

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
        using var content = new StreamContent(documentStream);
        content.Headers.ContentType = new MediaTypeHeaderValue(GetContentType(fileName));

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