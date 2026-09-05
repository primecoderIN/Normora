using System.Text.RegularExpressions;

namespace Normora.Modules.Documents.Persistence;

public static partial class DocumentChunker
{
    private const int MaximumChunkCharacters = 4_000;

    public static IReadOnlyList<string> Split(string extractedText)
    {
        var normalizedText = Normalize(extractedText);
        if (normalizedText.Length == 0)
        {
            return [];
        }

        var paragraphs = normalizedText.Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var chunks = new List<string>();
        var current = new List<string>();
        var currentLength = 0;

        foreach (var paragraph in paragraphs)
        {
            if (paragraph.Length > MaximumChunkCharacters)
            {
                Flush(chunks, current);
                currentLength = 0;

                for (var offset = 0; offset < paragraph.Length; offset += MaximumChunkCharacters)
                {
                    chunks.Add(paragraph.Substring(offset, Math.Min(MaximumChunkCharacters, paragraph.Length - offset)));
                }

                continue;
            }

            var separatorLength = current.Count == 0 ? 0 : 2;
            if (currentLength + separatorLength + paragraph.Length > MaximumChunkCharacters)
            {
                Flush(chunks, current);
                currentLength = 0;
            }

            current.Add(paragraph);
            currentLength += (current.Count == 1 ? 0 : 2) + paragraph.Length;
        }

        Flush(chunks, current);
        return chunks;
    }

    private static void Flush(List<string> chunks, List<string> current)
    {
        if (current.Count > 0)
        {
            chunks.Add(string.Join("\n\n", current));
            current.Clear();
        }
    }

    private static string Normalize(string text)
    {
        return WhitespaceRegex().Replace(text.Replace("\r\n", "\n").Replace('\r', '\n'), " ")
            .Replace(" \n", "\n")
            .Trim();
    }

    [GeneratedRegex("[ \\t]+")]
    private static partial Regex WhitespaceRegex();
}