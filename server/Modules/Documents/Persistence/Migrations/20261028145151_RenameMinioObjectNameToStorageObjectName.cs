using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Normora.Modules.Documents.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameMinioObjectNameToStorageObjectName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Documents_TenantId",
                table: "Documents");

            migrationBuilder.RenameColumn(
                name: "MinioObjectName",
                table: "DocumentVersions",
                newName: "StorageObjectName");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_TenantId_DeletedAt",
                table: "Documents",
                columns: new[] { "TenantId", "DeletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentChunks_Embedding",
                table: "DocumentChunks",
                column: "Embedding")
                .Annotation("Npgsql:IndexMethod", "hnsw")
                .Annotation("Npgsql:IndexOperators", new[] { "vector_cosine_ops" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Documents_TenantId_DeletedAt",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_DocumentChunks_Embedding",
                table: "DocumentChunks");

            migrationBuilder.RenameColumn(
                name: "StorageObjectName",
                table: "DocumentVersions",
                newName: "MinioObjectName");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_TenantId",
                table: "Documents",
                column: "TenantId");
        }
    }
}
