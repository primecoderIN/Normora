using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Conversations.Application.Exports;
using Normora.Modules.Conversations.Persistence;
using Normora.Shared.Exceptions;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Application.Queries;

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

public class ExportSavedAnswerQueryHandler(
    ConversationsDbContext context,
    ICurrentUser currentUser,
    IEnumerable<IAnswerExporter> exporters) : IRequestHandler<ExportSavedAnswerQuery, ExportResult>
{
    // Build a lookup once when the handler is instantiated (handler is scoped).
    private readonly Dictionary<ExportFormat, IAnswerExporter> _exporterMap = exporters
        .ToDictionary(e => e.Format);

    public async Task<ExportResult> Handle(ExportSavedAnswerQuery request, CancellationToken cancellationToken)
    {
        if (!_exporterMap.TryGetValue(request.Format, out var exporter))
            throw new InvalidOperationException($"Export format '{request.Format}' is not supported.");

        // Fetch the saved answer — the EF Core global query filter already enforces tenant isolation.
        var savedAnswer = await context.SavedAnswers
            .AsNoTracking()
            .Include(s => s.Message)
                .ThenInclude(m => m.Citations)
            .Include(s => s.Message)
                .ThenInclude(m => m.Conversation)
            .FirstOrDefaultAsync(
                s => s.Id == request.SavedAnswerId &&
                     s.UserId == currentUser.KeycloakUserId,
                cancellationToken)
            ?? throw new Normora.Shared.Exceptions.BolaException();

        // Resolve the preceding user question from the same conversation.
        var question = await context.Messages
            .AsNoTracking()
            .Where(m =>
                m.ConversationId == savedAnswer.ConversationId &&
                m.Role == Domain.MessageRole.User &&
                m.CreatedAt < savedAnswer.Message.CreatedAt)
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => m.Content)
            .FirstOrDefaultAsync(cancellationToken)
            ?? "Question not available.";

        var exportData = new AnswerExportData(
            Question: question,
            Answer: savedAnswer.Message.Content,
            Citations: savedAnswer.Message.Citations
                .OrderBy(c => c.Rank)
                .Select(c => new CitationExportData(c.FileName, c.Score))
                .ToList(),
            SavedAt: savedAnswer.CreatedAt,
            EmployeeName: currentUser.DisplayName ?? currentUser.Email ?? "Employee",
            ConversationTitle: savedAnswer.Message.Conversation.Title);

        return exporter.Export(exportData);
    }
}
