namespace Normora.Shared;

using Pgvector;

/// <summary>
/// A searchable piece of extracted document text.
/// </summary>
public class DocumentChunk
{
    public Guid Id { get; set; }
    /// Document Versioning (Phase 19): FK now points to DocumentVersion instead of Document.
    /// This allows chunks to be tied to a specific immutable file version.
    public Guid DocumentVersionId { get; set; }
    public Guid TenantId { get; set; }
    public int ChunkIndex { get; set; }
    public string Content { get; set; } = string.Empty;
    public Vector? Embedding { get; set; }
    public NpgsqlTypes.NpgsqlTsVector? SearchVector { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}