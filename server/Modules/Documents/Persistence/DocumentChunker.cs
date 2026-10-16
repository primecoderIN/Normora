using System.Text.RegularExpressions;

namespace Normora.Modules.Documents.Persistence;

/// <summary>
/// Utility class responsible for splitting raw document text into bounded, normalized chunks suitable for vector embeddings and retrieval.
/// </summary>
public static partial class DocumentChunker
{
    // This bound keeps individual retrieval and embedding inputs predictable; oversized
    // paragraphs are hard-split only when preserving the paragraph would exceed the limit.
    private const int MaximumChunkCharacters = 4_000;

    /// <summary>
    /// Splits the extracted text into an array of chunks, preserving paragraph boundaries where possible.
    /// </summary>
    /// <param name="extractedText">The raw extracted text.</param>
    /// <returns>A list of bounded text chunks.</returns>
    public static IReadOnlyList<string> Split(string extractedText)
    {
        var normalizedText = Normalize(extractedText);
        if (normalizedText.Length == 0)
        {
            return [];
        }

        // Pack complete paragraphs together first so search results retain local context.
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