using System.Text;
using System.Text.RegularExpressions;

namespace Normora.Modules.Documents.Persistence;

/// <summary>
/// Markdown and table-aware document chunker responsible for splitting extracted document text
/// into bounded, context-preserved chunks suitable for vector embeddings and hybrid retrieval.
/// Preserves Markdown headings (#, ##, ###), keeps tables atomic, and retains breadcrumb context.
/// </summary>
public static partial class DocumentChunker
{
    // Maximum chunk character bound keeping individual pgvector embedding and retrieval inputs predictable.
    private const int MaximumChunkCharacters = 4_000;

    // Semantic Overlap Window: retains trailing context (up to 200 characters) across non-table chunk boundaries.
    private const int ChunkOverlapCharacters = 200;

    /// <summary>
    /// Splits extracted text into bounded chunks, preserving Markdown hierarchy, tables, and paragraph boundaries
    /// with sliding overlap and section breadcrumbs.
    /// </summary>
    /// <param name="extractedText">The raw or Markdown-formatted extracted text.</param>
    /// <returns>A list of bounded text chunks.</returns>
    public static IReadOnlyList<string> Split(string extractedText)
    {
        var normalizedText = Normalize(extractedText);
        if (normalizedText.Length == 0)
        {
            return [];
        }

        // Split text into structural blocks (paragraphs, tables, headings)
        var rawBlocks = normalizedText.Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var chunks = new List<string>();
        var currentBlockList = new List<string>();
        var currentLength = 0;
        var headingStack = new List<(int Level, string Title)>();

        foreach (var block in rawBlocks)
        {
            // 1. Detect if the block is or starts with a Markdown heading
            UpdateHeadingHierarchy(block, headingStack);

            // 2. Check if the block is a Markdown table
            if (IsMarkdownTable(block))
            {
                HandleTableBlock(block, chunks, currentBlockList, ref currentLength, headingStack);
                continue;
            }

            // 3. Check for oversized single paragraph (larger than MaximumChunkCharacters)
            if (block.Length > MaximumChunkCharacters)
            {
                Flush(chunks, currentBlockList);
                currentLength = 0;

                var breadcrumb = FormatBreadcrumb(headingStack);
                var prefix = string.IsNullOrEmpty(breadcrumb) ? string.Empty : $"[{breadcrumb}]\n\n";
                var availableCapacity = MaximumChunkCharacters - prefix.Length;
                if (availableCapacity < 500) availableCapacity = MaximumChunkCharacters;

                var step = availableCapacity - ChunkOverlapCharacters;
                for (var offset = 0; offset < block.Length; offset += step)
                {
                    var len = Math.Min(availableCapacity, block.Length - offset);
                    var slice = block.Substring(offset, len);
                    chunks.Add(string.IsNullOrEmpty(prefix) || slice.StartsWith('#') ? slice : prefix + slice);
                    if (offset + len >= block.Length)
                        break;
                }

                continue;
            }

            // 4. Standard paragraph packing
            var separatorLength = currentBlockList.Count == 0 ? 0 : 2;
            if (currentLength + separatorLength + block.Length > MaximumChunkCharacters)
            {
                var flushedChunk = Flush(chunks, currentBlockList);
                currentLength = 0;

                // Carry over trailing sliding overlap if flushed chunk didn't end with a table
                if (!string.IsNullOrEmpty(flushedChunk) && flushedChunk.Length > ChunkOverlapCharacters)
                {
                    var overlap = flushedChunk[^ChunkOverlapCharacters..].TrimStart();
                    if (!string.IsNullOrWhiteSpace(overlap) && !overlap.StartsWith('|'))
                    {
                        currentBlockList.Add(overlap);
                        currentLength = overlap.Length;
                    }
                }
            }

            currentBlockList.Add(block);
            currentLength += (currentBlockList.Count == 1 ? 0 : 2) + block.Length;
        }

        Flush(chunks, currentBlockList);
        return chunks;
    }

    private static void HandleTableBlock(
        string tableBlock,
        List<string> chunks,
        List<string> currentBlockList,
        ref int currentLength,
        List<(int Level, string Title)> headingStack)
    {
        // If the table fits within MaximumChunkCharacters
        if (tableBlock.Length <= MaximumChunkCharacters)
        {
            // If it doesn't fit in the current accumulated chunk, flush first to keep the table whole
            var separatorLength = currentBlockList.Count == 0 ? 0 : 2;
            if (currentLength + separatorLength + tableBlock.Length > MaximumChunkCharacters)
            {
                Flush(chunks, currentBlockList);
                currentLength = 0;
            }

            currentBlockList.Add(tableBlock);
            currentLength += (currentBlockList.Count == 1 ? 0 : 2) + tableBlock.Length;
            return;
        }

        // Table itself exceeds MaximumChunkCharacters — flush any existing chunk first
        Flush(chunks, currentBlockList);
        currentLength = 0;

        // Split oversized table row-by-row while preserving header on each slice
        var lines = tableBlock.Split('\n', StringSplitOptions.TrimEntries);
        if (lines.Length < 3)
        {
            // Not a well-formed table, fallback to standard splitting
            chunks.Add(tableBlock[..Math.Min(tableBlock.Length, MaximumChunkCharacters)]);
            return;
        }

        var header = lines[0] + "\n" + lines[1]; // Header row + separator row (|---|---|)
        var breadcrumb = FormatBreadcrumb(headingStack);
        var tablePrefix = string.IsNullOrEmpty(breadcrumb) ? header : $"[{breadcrumb}]\n\n" + header;

        var currentTableSlice = new StringBuilder(tablePrefix);
        for (var i = 2; i < lines.Length; i++)
        {
            var line = lines[i];
            if (currentTableSlice.Length + 1 + line.Length > MaximumChunkCharacters)
            {
                chunks.Add(currentTableSlice.ToString());
                currentTableSlice.Clear();
                currentTableSlice.AppendLine(tablePrefix);
            }

            currentTableSlice.AppendLine(line);
        }

        if (currentTableSlice.Length > tablePrefix.Length)
        {
            chunks.Add(currentTableSlice.ToString().TrimEnd());
        }
    }

    private static void UpdateHeadingHierarchy(string block, List<(int Level, string Title)> stack)
    {
        var firstLine = block.Split('\n', 2)[0].Trim();
        var match = HeadingRegex().Match(firstLine);
        if (!match.Success) return;

        var level = match.Groups[1].Value.Length;
        var title = match.Groups[2].Value.Trim();

        // Pop headings with equal or deeper level
        while (stack.Count > 0 && stack[^1].Level >= level)
        {
            stack.RemoveAt(stack.Count - 1);
        }

        stack.Add((level, title));
    }

    private static string FormatBreadcrumb(List<(int Level, string Title)> stack)
    {
        if (stack.Count == 0) return string.Empty;
        return string.Join(" > ", stack.Select(h => $"{new string('#', h.Level)} {h.Title}"));
    }

    private static bool IsMarkdownTable(string block)
    {
        var lines = block.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length < 2) return false;

        // Table must contain '|' and line 2 is typically the separator row (|---|---|)
        return lines[0].Contains('|') &&
               lines[1].Contains('|') &&
               lines[1].Contains('-');
    }

    private static string Flush(List<string> chunks, List<string> currentBlockList)
    {
        if (currentBlockList.Count == 0) return string.Empty;

        var result = string.Join("\n\n", currentBlockList);
        chunks.Add(result);
        currentBlockList.Clear();
        return result;
    }

    private static string Normalize(string text)
    {
        return WhitespaceRegex().Replace(text.Replace("\r\n", "\n").Replace('\r', '\n'), " ")
            .Replace(" \n", "\n")
            .Trim();
    }

    [GeneratedRegex("[ \\t]+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"^(#{1,6})\s+(.+)$")]
    private static partial Regex HeadingRegex();
}
