using Normora.Modules.Documents.Persistence;
using Xunit;

namespace Normora.Architecture.Tests;

public sealed class DocumentChunkerTests
{
    [Fact]
    public void Split_EmptyOrWhitespace_ReturnsEmptyList()
    {
        Assert.Empty(DocumentChunker.Split(string.Empty));
        Assert.Empty(DocumentChunker.Split("   \n\n  \t "));
    }

    [Fact]
    public void Split_StandardParagraphs_PacksWithinCharacterBound()
    {
        var text = "Paragraph 1: Welcome to Normora.\n\nParagraph 2: Employee Handbook overview.";
        var chunks = DocumentChunker.Split(text);

        Assert.Single(chunks);
        Assert.Contains("Paragraph 1", chunks[0]);
        Assert.Contains("Paragraph 2", chunks[0]);
    }

    [Fact]
    public void Split_MarkdownHeadings_MaintainsBreadcrumbHierarchy()
    {
        var markdown = """
            # Travel Policy

            ## Hotel Reimbursement

            Employees can book standard hotel rooms up to $250 per night.
            """;

        var chunks = DocumentChunker.Split(markdown);

        Assert.NotEmpty(chunks);
        Assert.Contains("Hotel Reimbursement", chunks[0]);
        Assert.Contains("$250", chunks[0]);
    }

    [Fact]
    public void Split_MarkdownTable_PreservesAtomicTableStructure()
    {
        var markdown = """
            # Benefits

            Here is the summary of allowances:

            | Tier | Meal Allowance | Hotel Cap |
            | :--- | :--- | :--- |
            | Tier 1 | $100 | $300 |
            | Tier 2 | $75 | $200 |

            All claims must include receipts.
            """;

        var chunks = DocumentChunker.Split(markdown);

        Assert.NotEmpty(chunks);
        var tableChunk = chunks.FirstOrDefault(c => c.Contains("| Tier |"));
        Assert.NotNull(tableChunk);
        Assert.Contains("| Tier 1 | $100 | $300 |", tableChunk);
        Assert.Contains("| Tier 2 | $75 | $200 |", tableChunk);
    }

    [Fact]
    public void Split_OversizedTable_SplitsRowsWhilePreservingHeaderOnEachSlice()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("# Equipment Inventory");
        sb.AppendLine();
        sb.AppendLine("| Item ID | Description | Allocation Cost | Approval Required |");
        sb.AppendLine("| :--- | :--- | :--- | :--- |");

        // Generate 150 table rows to exceed the 4,000 character bound
        for (var i = 1; i <= 150; i++)
        {
            sb.AppendLine($"| EQ-{i:D4} | High Performance Developer Laptop Model Standard Edition Series | $3,500.00 | Manager Signoff |");
        }

        var chunks = DocumentChunker.Split(sb.ToString());

        Assert.True(chunks.Count > 1, "Expected table to be split across multiple chunks");
        foreach (var chunk in chunks)
        {
            Assert.True(chunk.Length <= 4_000, $"Chunk exceeded 4,000 characters: {chunk.Length}");
            // Each chunk slice containing table data must retain the column headers
            Assert.Contains("| Item ID | Description |", chunk);
            Assert.Contains("| :--- | :--- |", chunk);
        }
    }
}

