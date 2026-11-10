using System.IO.Compression;
using System.Net.Http.Json;
using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Normora.Infrastructure.Providers.Gemini;
using Normora.Modules.Documents.Persistence;
using Normora.Shared.Constants;

namespace Normora.Infrastructure.Providers.DocumentExtraction;

/// <summary>
/// Implementation of <see cref="IDocumentTextExtractor"/> that uses Google's Gemini multimodal AI
/// for OCR, document layout analysis, and Markdown table extraction from PDFs and images,
/// with fast in-memory extraction for native text (.txt) and OpenXML documents (.docx).
/// </summary>
public sealed class GeminiDocumentTextExtractor(
    HttpClient httpClient,
    IOptions<GeminiOptions> options,
    ILogger<GeminiDocumentTextExtractor> logger) : IDocumentTextExtractor
{
    private const string ExtractionPrompt =
        "Extract all readable text from this document verbatim. " +
        "Preserve document hierarchy using Markdown headings (#, ##, ###) and lists (-). " +
        "Format tables as clean Markdown tables. " +
        "Do not summarize, explain, add commentary, or omit text.";

    private readonly GeminiOptions geminiOptions = options.Value;

    public async Task<string> ExtractAsync(
        Stream documentStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();

        return ext switch
        {
            ".txt" => await ExtractPlainTextAsync(documentStream, cancellationToken),
            ".docx" => await ExtractWordDocumentAsync(documentStream, cancellationToken),
            ".pdf" => await ExtractViaGeminiAsync(documentStream, "application/pdf", fileName, cancellationToken),
            ".png" => await ExtractViaGeminiAsync(documentStream, "image/png", fileName, cancellationToken),
            ".jpg" or ".jpeg" => await ExtractViaGeminiAsync(documentStream, "image/jpeg", fileName, cancellationToken),
            ".webp" => await ExtractViaGeminiAsync(documentStream, "image/webp", fileName, cancellationToken),
            _ => throw new InvalidOperationException(ApiMessages.InvalidFileType)
        };
    }

    private static async Task<string> ExtractPlainTextAsync(Stream documentStream, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(documentStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    private static async Task<string> ExtractWordDocumentAsync(Stream documentStream, CancellationToken cancellationToken)
    {
        using var archive = new ZipArchive(documentStream, ZipArchiveMode.Read, leaveOpen: true);
        var entry = archive.GetEntry("word/document.xml");
        if (entry is null)
        {
            return string.Empty;
        }

        await using var entryStream = entry.Open();
        var doc = await XDocument.LoadAsync(entryStream, LoadOptions.None, cancellationToken);
        XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

        var sb = new StringBuilder();
        foreach (var p in doc.Descendants(w + "p"))
        {
            var text = string.Concat(p.Descendants(w + "t").Select(t => t.Value));
            if (!string.IsNullOrWhiteSpace(text))
            {
                sb.AppendLine(text);
            }
        }

        return sb.ToString().TrimEnd();
    }

    private async Task<string> ExtractViaGeminiAsync(
        Stream documentStream,
        string mimeType,
        string fileName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(geminiOptions.ApiKey))
        {
            throw new InvalidOperationException(ApiMessages.ExtractionServiceNotConfigured);
        }

        using var memoryStream = new MemoryStream();
        await documentStream.CopyToAsync(memoryStream, cancellationToken);
        var base64Data = Convert.ToBase64String(memoryStream.ToArray());

        var model = !string.IsNullOrWhiteSpace(geminiOptions.ExtractionModel)
            ? geminiOptions.ExtractionModel
            : geminiOptions.GenerationModel;

        var request = new GeminiGenerateRequest(
            [
                new GeminiContent(
                [
                    new GeminiPart(ExtractionPrompt),
                    new GeminiPart(new GeminiInlineData(mimeType, base64Data))
                ])
            ],
            new GeminiGenerationConfig(0.0));

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"models/{model}:generateContent")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Add("x-goog-api-key", geminiOptions.ApiKey);

        logger.LogInformation(
            "Invoking Gemini multimodal extraction model {Model} for {FileName} ({MimeType}, {Bytes} bytes).",
            model,
            fileName,
            mimeType,
            memoryStream.Length);

        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError(
                "Gemini extraction failed for {FileName} ({Model}) with status {StatusCode}: {ErrorBody}",
                fileName,
                model,
                response.StatusCode,
                errorBody);
        }
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<GeminiGenerateResponse>(cancellationToken);
        var extractedParts = result?.Candidates?
            .FirstOrDefault()?
            .Content?
            .Parts?
            .Where(p => !string.IsNullOrWhiteSpace(p.Text))
            .Select(p => p.Text!.Trim())
            .ToList();

        var extractedText = extractedParts is { Count: > 0 }
            ? string.Join("\n\n", extractedParts)
            : string.Empty;

        return extractedText.Trim();
    }
}

