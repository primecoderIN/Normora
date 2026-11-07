using MediatR;
using Normora.Modules.Conversations.Application.Exports;

namespace Normora.Modules.Conversations.Application.SavedAnswers.Queries;

/// <summary>
/// Supported binary and textual export formats for a saved answer.
/// </summary>
public enum ExportFormat
{
    Markdown,
    Pdf,
    Docx
}

/// <summary>
/// Generates an exportable document file for a single saved answer identified by its <see cref="SavedAnswerId"/>.
/// Handled by <see cref="ExportSavedAnswerQueryHandler"/> to provide downloadable policy summaries.
/// </summary>
/// <remarks>
/// Document Generation Pipeline:
/// - Verifies that the saved answer belongs to the requesting caller (BOLA protection).
/// - Delegates file creation to format-specific exporters (<see cref="PdfAnswerExporter"/>, <see cref="DocxAnswerExporter"/>, <see cref="MarkdownAnswerExporter"/>).
/// - Embeds source citations, timestamps, and workspace attribution into the generated document.
/// </remarks>
public record ExportSavedAnswerQuery(Guid SavedAnswerId, ExportFormat Format)
    : IRequest<ExportResult>;

