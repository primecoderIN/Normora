using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Normora.Modules.Conversations.Application.Queries;

namespace Normora.Modules.Conversations.Application.Exports;

/// <summary>
/// Exports a saved answer as a Word (.docx) document using the Open XML SDK.
/// Produces a clean, structured document with proper heading styles, paragraph spacing,
/// a citations table, and a footer — all without requiring Microsoft Word.
/// </summary>
public class DocxAnswerExporter : IAnswerExporter
{
    // Pre-compiled regex patterns — allocated once per singleton, reused on every export call.
    private static readonly Regex HeadingRegex   = new(@"^#{1,6}\s+",        RegexOptions.Multiline | RegexOptions.Compiled);
    private static readonly Regex BoldItalicRegex = new(@"\*{1,3}(.+?)\*{1,3}", RegexOptions.Compiled);
    private static readonly Regex UnderscoreRegex = new(@"_{1,3}(.+?)_{1,3}",   RegexOptions.Compiled);
    private static readonly Regex InlineCodeRegex = new(@"`([^`]+)`",           RegexOptions.Compiled);
    private static readonly Regex LinkRegex       = new(@"\[(.+?)\]\(.+?\)",   RegexOptions.Compiled);
    private static readonly Regex ListBulletRegex = new(@"^\s*[-*+]\s+",        RegexOptions.Multiline | RegexOptions.Compiled);

    public ExportFormat Format => ExportFormat.Docx;
    public string ContentType => "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
    public string FileExtension => ".docx";

    public ExportResult Export(AnswerExportData data)
    {
        using var stream = new MemoryStream();
        using (var wordDocument = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var mainPart = wordDocument.AddMainDocumentPart();
            mainPart.Document = new Document(BuildBody(data));

            // Add numbered list style for citations
            var stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
            stylesPart.Styles = BuildStyles();
            stylesPart.Styles.Save();
        }

        return new ExportResult(stream.ToArray(), ContentType, FileExtension);
    }

    // ── Body ──────────────────────────────────────────────────────────────────

    private static Body BuildBody(AnswerExportData data)
    {
        var body = new Body();

        // Title
        body.AppendChild(Heading1("Normora — Saved Answer"));

        // Metadata
        body.AppendChild(MetaLine("Conversation:", data.ConversationTitle));
        body.AppendChild(MetaLine("Employee:", data.EmployeeName));
        body.AppendChild(MetaLine("Saved:", $"{data.SavedAt:dddd, MMMM d, yyyy 'at' h:mm tt} UTC"));
        body.AppendChild(HorizontalRule());

        // Question section
        body.AppendChild(Heading2("Question"));
        body.AppendChild(QuoteParagraph(data.Question));

        // Answer section
        body.AppendChild(Heading2("Answer"));
        foreach (var line in SplitParagraphs(data.Answer))
        {
            body.AppendChild(NormalParagraph(line));
        }

        // Citations section
        if (data.Citations.Count > 0)
        {
            body.AppendChild(HorizontalRule());
            body.AppendChild(Heading2("Sources"));
            body.AppendChild(BuildCitationsTable(data.Citations));
        }

        // Footer paragraph
        body.AppendChild(HorizontalRule());
        body.AppendChild(FooterParagraph("Exported from Normora — AI-powered company knowledge assistant."));

        // Required section properties for Open XML
        body.AppendChild(new SectionProperties(
            new PageSize { Width = 11906, Height = 16838 },
            new PageMargin { Top = 1134, Right = 1134, Bottom = 1134, Left = 1134 }
        ));

        return body;
    }

    // ── Paragraph builders ───────────────────────────────────────────────────

    private static Paragraph Heading1(string text) =>
        new(
            new ParagraphProperties(
                new ParagraphStyleId { Val = "Heading1" },
                new SpacingBetweenLines { After = "240" }),
            new Run(
                new RunProperties(
                    new Bold(),
                    new FontSize { Val = "36" },
                    new Color { Val = "6366f1" }),
                new Text(text)));

    private static Paragraph Heading2(string text) =>
        new(
            new ParagraphProperties(
                new SpacingBetweenLines { Before = "360", After = "120" }),
            new Run(
                new RunProperties(
                    new Bold(),
                    new FontSize { Val = "26" },
                    new Color { Val = "0f172a" }),
                new Text(text)));

    private static Paragraph MetaLine(string label, string value) =>
        new(
            new ParagraphProperties(new SpacingBetweenLines { After = "60" }),
            new Run(
                new RunProperties(new Bold(), new Color { Val = "475569" }),
                new Text(label) { Space = SpaceProcessingModeValues.Preserve }),
            new Run(
                new Text(" " + value) { Space = SpaceProcessingModeValues.Preserve }));

    private static Paragraph QuoteParagraph(string text) =>
        new(
            new ParagraphProperties(
                new Indentation { Left = "720" },
                new ParagraphBorders(
                    new LeftBorder
                    {
                        Val = BorderValues.Single,
                        Color = "6366f1",
                        Size = 18,
                        Space = 10
                    }),
                new SpacingBetweenLines { Before = "120", After = "240" }),
            new Run(
                new RunProperties(new Italic(), new Color { Val = "334155" }),
                new Text(text)));

    private static Paragraph NormalParagraph(string text) =>
        new(
            new ParagraphProperties(new SpacingBetweenLines { After = "160" }),
            new Run(new Text(text) { Space = SpaceProcessingModeValues.Preserve }));

    private static Paragraph HorizontalRule() =>
        new(
            new ParagraphProperties(
                new ParagraphBorders(
                    new BottomBorder { Val = BorderValues.Single, Color = "e2e8f0", Size = 6 }),
                new SpacingBetweenLines { Before = "240", After = "240" }));

    private static Paragraph FooterParagraph(string text) =>
        new(
            new ParagraphProperties(
                new SpacingBetweenLines { Before = "240" }),
            new Run(
                new RunProperties(
                    new Italic(),
                    new FontSize { Val = "18" },
                    new Color { Val = "94a3b8" }),
                new Text(text)));

    private static Table BuildCitationsTable(IReadOnlyList<CitationExportData> citations)
    {
        var table = new Table();

        // Table properties
        table.AppendChild(new TableProperties(
            new TableBorders(
                new TopBorder { Val = BorderValues.Single, Color = "e2e8f0", Size = 4 },
                new BottomBorder { Val = BorderValues.Single, Color = "e2e8f0", Size = 4 },
                new LeftBorder { Val = BorderValues.Single, Color = "e2e8f0", Size = 4 },
                new RightBorder { Val = BorderValues.Single, Color = "e2e8f0", Size = 4 },
                new InsideHorizontalBorder { Val = BorderValues.Single, Color = "e2e8f0", Size = 4 },
                new InsideVerticalBorder { Val = BorderValues.Single, Color = "e2e8f0", Size = 4 }),
            new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct }));

        // Header row
        var headerRow = new TableRow();
        headerRow.AppendChild(TableCell("Document", bold: true, bg: "f8fafc"));
        headerRow.AppendChild(TableCell("Relevance", bold: true, bg: "f8fafc", right: true));
        table.AppendChild(headerRow);

        // Data rows
        foreach (var cite in citations)
        {
            var row = new TableRow();
            row.AppendChild(TableCell(cite.FileName));
            row.AppendChild(TableCell($"{(int)Math.Round(cite.Score * 100)}%", color: "6366f1", right: true));
            table.AppendChild(row);
        }

        return table;
    }

    private static TableCell TableCell(
        string text,
        bool bold = false,
        string? bg = null,
        string? color = null,
        bool right = false)
    {
        var runProps = new RunProperties();
        if (bold) runProps.AppendChild(new Bold());
        if (color != null) runProps.AppendChild(new Color { Val = color });
        runProps.AppendChild(new FontSize { Val = "18" });

        var paraProps = new ParagraphProperties(
            new SpacingBetweenLines { Before = "80", After = "80" });
        if (right) paraProps.AppendChild(new Justification { Val = JustificationValues.Right });

        var para = new Paragraph(paraProps, new Run(runProps, new Text(text)));

        var cellProps = new TableCellProperties(
            new TableCellMargin(
                new LeftMargin { Width = "100", Type = TableWidthUnitValues.Dxa },
                new RightMargin { Width = "100", Type = TableWidthUnitValues.Dxa }));
        if (bg != null) cellProps.AppendChild(new Shading { Fill = bg, Val = ShadingPatternValues.Clear });

        var cell = new TableCell(cellProps, para);
        return cell;
    }

    // ── Styles ───────────────────────────────────────────────────────────────

    private static Styles BuildStyles()
    {
        var styles = new Styles();
        styles.DocDefaults = new DocDefaults(
            new RunPropertiesDefault(
                new RunPropertiesBaseStyle(
                    new RunFonts { Ascii = "Arial", HighAnsi = "Arial" },
                    new FontSize { Val = "20" })));
        return styles;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Splits a Markdown answer into paragraphs by double-newline boundaries,
    /// stripping simple Markdown symbols using pre-compiled patterns.
    /// </summary>
    private static IEnumerable<string> SplitParagraphs(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return [""];

        var stripped = HeadingRegex.Replace(markdown, string.Empty);
        stripped = BoldItalicRegex.Replace(stripped, "$1");
        stripped = UnderscoreRegex.Replace(stripped, "$1");
        stripped = InlineCodeRegex.Replace(stripped, "$1");
        stripped = LinkRegex.Replace(stripped, "$1");
        stripped = ListBulletRegex.Replace(stripped, "• ");

        return stripped
            .Split(["\n\n", "\r\n\r\n"], StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .Where(p => !string.IsNullOrEmpty(p));
    }
}
