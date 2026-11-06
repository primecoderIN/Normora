using MediatR;
using Normora.Modules.Conversations.Application.Exports;

namespace Normora.Modules.Conversations.Application.SavedAnswers.Queries;

/// <summary>
/// Supported export formats for a saved answer.
/// </summary>
public enum ExportFormat
{
    Markdown,
    Pdf,
    Docx
}

/// <summary>
/// Generates an exportable file for a single saved answer identified by its <see cref="SavedAnswerId"/>.
/// Returns an <see cref="ExportResult"/> containing the raw bytes, content-type, and file extension.
/// </summary>
public record ExportSavedAnswerQuery(Guid SavedAnswerId, ExportFormat Format)
    : IRequest<ExportResult>;

