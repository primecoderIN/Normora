using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pgvector.EntityFrameworkCore;
using Normora.Modules.Documents.Domain;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Documents.Persistence.Configurations;

public class DocumentChunkConfiguration : IEntityTypeConfiguration<DocumentChunk>
{
    public void Configure(EntityTypeBuilder<DocumentChunk> builder)
    {
        builder.HasKey(chunk => chunk.Id);
        builder.Property(chunk => chunk.Content).IsRequired().HasColumnType("text");
        builder.Property(chunk => chunk.Embedding).HasColumnType("vector(768)");

        // Full-Text Search Vector:
        // Generates a tsvector column using PostgreSQL English stemming and indexes it with a GIN index.
        // Enables BM25/keyword ranking to be fused with dense semantic vector search via Reciprocal Rank Fusion (RRF).
        builder.HasGeneratedTsVectorColumn(
            chunk => chunk.SearchVector!,
            "english",
            chunk => new { chunk.Content })
        .HasIndex(chunk => chunk.SearchVector)
        .HasMethod("GIN");

        // Vector Similarity HNSW Index:
        // Creates a Hierarchical Navigable Small World (HNSW) index using cosine distance operators (vector_cosine_ops).
        // Delivers sub-millisecond approximate nearest neighbor lookups under enterprise chunk volume without full table scans.
        builder.HasIndex(chunk => chunk.Embedding)
            .HasMethod("hnsw")
            .HasOperators("vector_cosine_ops");

        // Composite uniqueness constraint guarantees deterministic chunk ordering per document version within a tenant.
        builder.HasIndex(chunk => new { chunk.TenantId, chunk.DocumentVersionId, chunk.ChunkIndex }).IsUnique();

        // Cascade delete ensures physical cleanup of a DocumentVersion automatically wipes associated vector chunks.
        builder.HasOne<DocumentVersion>()
            .WithMany(v => v.Chunks)
            .HasForeignKey(chunk => chunk.DocumentVersionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

