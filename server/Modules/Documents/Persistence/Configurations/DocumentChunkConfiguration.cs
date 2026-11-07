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

        // Configure full-text search vector
        builder.HasGeneratedTsVectorColumn(
            chunk => chunk.SearchVector!,
            "english",
            chunk => new { chunk.Content })
        .HasIndex(chunk => chunk.SearchVector)
        .HasMethod("GIN");

        // Vector similarity HNSW index for high-performance approximate nearest neighbor retrieval
        builder.HasIndex(chunk => chunk.Embedding)
            .HasMethod("hnsw")
            .HasOperators("vector_cosine_ops");

        builder.HasIndex(chunk => new { chunk.TenantId, chunk.DocumentVersionId, chunk.ChunkIndex }).IsUnique();

        builder.HasOne<DocumentVersion>()
            .WithMany(v => v.Chunks)
            .HasForeignKey(chunk => chunk.DocumentVersionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

