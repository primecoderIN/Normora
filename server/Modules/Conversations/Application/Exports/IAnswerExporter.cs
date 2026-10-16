namespace Normora.Modules.Conversations.Application.Exports;

/// <summary>
/// Represents an exported answer as a raw byte payload with its associated content-type and file-name suffix.
/// </summary>
public record ExportResult(byte[] Bytes, string ContentType, string FileExtension);

/// <summary>
/// Defines the contract for exporting a saved answer to a specific document format.
/// Each implementation is responsible for one format (Markdown, PDF, DOCX).
/// Implementations are registered as singletons — they must be stateless.
/// </summary>
public interface IAnswerExporter
{
    /// <summary>
    /// The <see cref="ExportFormat"/> this exporter handles.
    /// Used by <c>ExportSavedAnswerQueryHandler</c> to resolve the correct implementation.
    /// </summary>
    Queries.ExportFormat Format { get; }

    /// <summary>The MIME type produced by this exporter, e.g. "text/markdown".</summary>
    string ContentType { get; }

    /// <summary>The file extension produced by this exporter, e.g. ".md".</summary>
    string FileExtension { get; }

    /// <summary>
    /// Generates the export payload from the supplied answer data.
    /// </summary>
    ExportResult Export(AnswerExportData data);
}

/// <summary>
/// All data required by any exporter to render a complete, self-contained export document.
/// </summary>
public record AnswerExportData(
    string Question,
    string Answer,
    IReadOnlyList<CitationExportData> Citations,
    DateTimeOffset SavedAt,
    string EmployeeName,
    string ConversationTitle);

/// <summary>Source citation data included in the export.</summary>
public record CitationExportData(string FileName, double Score);
