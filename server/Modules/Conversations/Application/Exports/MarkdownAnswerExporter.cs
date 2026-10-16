using System.Text;
using Normora.Modules.Conversations.Application.Queries;

namespace Normora.Modules.Conversations.Application.Exports;

/// <summary>
/// Exports a saved answer as a Markdown (.md) document.
/// No external dependencies — pure string building.
/// </summary>
public class MarkdownAnswerExporter : IAnswerExporter
{
    public ExportFormat Format => ExportFormat.Markdown;
    public string ContentType => "text/markdown; charset=utf-8";
    public string FileExtension => ".md";

    public ExportResult Export(AnswerExportData data)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine("# Normora — Saved Answer");
        sb.AppendLine();
        sb.AppendLine($"**Conversation:** {data.ConversationTitle}  ");
        sb.AppendLine($"**Employee:** {data.EmployeeName}  ");
        sb.AppendLine($"**Saved:** {data.SavedAt:dddd, MMMM d, yyyy 'at' h:mm tt} UTC");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();

        // Question
        sb.AppendLine("## Question");
        sb.AppendLine();
        sb.AppendLine($"> {data.Question}");
        sb.AppendLine();

        // Answer
        sb.AppendLine("## Answer");
        sb.AppendLine();
        sb.AppendLine(data.Answer.Trim());
        sb.AppendLine();

        // Citations
        if (data.Citations.Count > 0)
        {
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine("## Sources");
            sb.AppendLine();
            foreach (var cite in data.Citations)
            {
                var percent = (int)Math.Round(cite.Score * 100);
                sb.AppendLine($"- **{cite.FileName}** — {percent}% relevance");
            }
            sb.AppendLine();
        }

        // Footer
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("*Exported from [Normora](https://normora.io) — AI-powered company knowledge assistant.*");

        return new ExportResult(
            Encoding.UTF8.GetBytes(sb.ToString()),
            ContentType,
            FileExtension);
    }
}
