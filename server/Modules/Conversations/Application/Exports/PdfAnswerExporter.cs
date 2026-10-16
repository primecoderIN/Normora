using System.Text.RegularExpressions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Normora.Modules.Conversations.Application.Queries;

namespace Normora.Modules.Conversations.Application.Exports;

/// <summary>
/// Exports a saved answer as a PDF document using QuestPDF.
/// The layout is clean and professional, with a header, question block, answer body, and citations footer.
/// </summary>
public class PdfAnswerExporter : IAnswerExporter
{
    /// <summary>
    /// Sets the QuestPDF community licence once when the singleton is constructed.
    /// This must be done before any document is generated.
    /// </summary>
    static PdfAnswerExporter() => QuestPDF.Settings.License = LicenseType.Community;

    // Pre-compiled regex patterns — allocated once per singleton, reused on every export call.
    private static readonly Regex HeadingRegex    = new(@"^#{1,6}\s+",    RegexOptions.Multiline | RegexOptions.Compiled);
    private static readonly Regex BoldItalicRegex  = new(@"\*{1,3}(.+?)\*{1,3}", RegexOptions.Compiled);
    private static readonly Regex UnderscoreRegex  = new(@"_{1,3}(.+?)_{1,3}",   RegexOptions.Compiled);
    private static readonly Regex InlineCodeRegex  = new(@"`([^`]+)`",           RegexOptions.Compiled);
    private static readonly Regex LinkRegex        = new(@"\[(.+?)\]\(.+?\)",    RegexOptions.Compiled);
    private static readonly Regex ListBulletRegex  = new(@"^\s*[-*+]\s+",        RegexOptions.Multiline | RegexOptions.Compiled);

    public ExportFormat Format => ExportFormat.Pdf;
    public string ContentType => "application/pdf";
    public string FileExtension => ".pdf";

    public ExportResult Export(AnswerExportData data)
    {
        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(48, Unit.Point);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10).FontColor("#1e293b"));

                // ── Page Header ──────────────────────────────────────────────
                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Normora")
                            .Bold().FontSize(18).FontColor("#6366f1");

                        row.ConstantItem(200).AlignRight()
                            .Text($"Saved Answer — {data.ConversationTitle}")
                            .FontSize(9).FontColor("#64748b");
                    });

                    col.Item().PaddingTop(4).LineHorizontal(1).LineColor("#e2e8f0");
                });

                // ── Page Body ────────────────────────────────────────────────
                page.Content().PaddingTop(24).Column(col =>
                {
                    // Metadata row
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(meta =>
                        {
                            meta.Item().Text(t =>
                            {
                                t.Span("Employee: ").SemiBold().FontColor("#475569");
                                t.Span(data.EmployeeName).FontColor("#1e293b");
                            });
                            meta.Item().Text(t =>
                            {
                                t.Span("Saved: ").SemiBold().FontColor("#475569");
                                t.Span(data.SavedAt.ToString("MMMM d, yyyy 'at' h:mm tt") + " UTC").FontColor("#1e293b");
                            });
                        });
                    });

                    col.Item().PaddingTop(20).PaddingBottom(8)
                        .Text("Question").Bold().FontSize(13).FontColor("#0f172a");

                    // Question block
                    col.Item()
                        .Background("#f1f5f9")
                        .Border(1).BorderColor("#e2e8f0")
                        .Padding(12)
                        .Text(data.Question)
                        .FontSize(11).Italic().FontColor("#334155");

                    col.Item().PaddingTop(20).PaddingBottom(8)
                        .Text("Answer").Bold().FontSize(13).FontColor("#0f172a");

                    // Answer body — Markdown stripped for clean PDF rendering
                    col.Item().Text(StripMarkdown(data.Answer)).FontSize(10).LineHeight(1.6f).FontColor("#1e293b");

                    // Citations
                    if (data.Citations.Count > 0)
                    {
                        col.Item().PaddingTop(24).PaddingBottom(8)
                            .Text("Sources").Bold().FontSize(11).FontColor("#475569");

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(4);
                                cols.RelativeColumn(1);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background("#f8fafc").Padding(6)
                                    .Text("Document").Bold().FontSize(9).FontColor("#64748b");
                                header.Cell().Background("#f8fafc").Padding(6).AlignRight()
                                    .Text("Relevance").Bold().FontSize(9).FontColor("#64748b");
                            });

                            foreach (var cite in data.Citations)
                            {
                                table.Cell().BorderBottom(1).BorderColor("#f1f5f9").Padding(6)
                                    .Text(cite.FileName).FontSize(9).FontColor("#334155");
                                table.Cell().BorderBottom(1).BorderColor("#f1f5f9").Padding(6).AlignRight()
                                    .Text($"{(int)Math.Round(cite.Score * 100)}%").FontSize(9)
                                    .FontColor("#6366f1").Bold();
                            }
                        });
                    }
                });

                // ── Page Footer ──────────────────────────────────────────────
                page.Footer().AlignCenter()
                    .Text(t =>
                    {
                        t.Span("Exported from Normora — AI-powered company knowledge assistant  |  Page ")
                            .FontSize(8).FontColor("#94a3b8");
                        t.CurrentPageNumber().FontSize(8).FontColor("#94a3b8");
                        t.Span(" of ").FontSize(8).FontColor("#94a3b8");
                        t.TotalPages().FontSize(8).FontColor("#94a3b8");
                    });
            });
        }).GeneratePdf();

        return new ExportResult(bytes, ContentType, FileExtension);
    }

    /// <summary>
    /// Strips common Markdown syntax tokens for inline PDF rendering using pre-compiled patterns.
    /// A full Markdown-to-PDF renderer is out of scope; this keeps the text readable.
    /// </summary>
    private static string StripMarkdown(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown)) return string.Empty;

        var text = HeadingRegex.Replace(markdown, string.Empty);
        text = BoldItalicRegex.Replace(text, "$1");
        text = UnderscoreRegex.Replace(text, "$1");
        text = InlineCodeRegex.Replace(text, "$1");
        text = LinkRegex.Replace(text, "$1");
        text = ListBulletRegex.Replace(text, "• ");
        return text.Trim();
    }
}
