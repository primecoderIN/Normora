using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Normora.Modules.Documents.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentVersioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Drop the old FK from DocumentChunks -> Documents.
            migrationBuilder.DropForeignKey(
                name: "FK_DocumentChunks_Documents_DocumentId",
                table: "DocumentChunks");

            // Step 2: Create the DocumentVersions table BEFORE removing columns from Documents,
            // so we can copy the existing MinioObjectName, ExtractedText, and Status into it.
            migrationBuilder.CreateTable(
                name: "DocumentVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    MinioObjectName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ExtractedText = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentVersions_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentVersions_DocumentId",
                table: "DocumentVersions",
                column: "DocumentId");

            // Document Versioning (Phase 19): DATA MIGRATION
            // Step 3: Insert a "Version 1" row into DocumentVersions for each existing Document,
            // copying over the MinioObjectName, ExtractedText, and Status that are about to be dropped.
            migrationBuilder.Sql(@"
                INSERT INTO ""DocumentVersions"" (""Id"", ""DocumentId"", ""TenantId"", ""VersionNumber"", ""MinioObjectName"", ""ExtractedText"", ""Status"", ""IsActive"", ""CreatedAt"")
                SELECT gen_random_uuid(), d.""Id"", d.""TenantId"", 1, d.""MinioObjectName"", d.""ExtractedText"", d.""Status"", true, d.""UploadedAt""
                FROM ""Documents"" d;
            ");

            // Step 4: Rename DocumentChunks.DocumentId -> DocumentVersionId and update its indexes.
            migrationBuilder.RenameColumn(
                name: "DocumentId",
                table: "DocumentChunks",
                newName: "DocumentVersionId");

            migrationBuilder.RenameIndex(
                name: "IX_DocumentChunks_TenantId_DocumentId_ChunkIndex",
                table: "DocumentChunks",
                newName: "IX_DocumentChunks_TenantId_DocumentVersionId_ChunkIndex");

            migrationBuilder.RenameIndex(
                name: "IX_DocumentChunks_DocumentId",
                table: "DocumentChunks",
                newName: "IX_DocumentChunks_DocumentVersionId");

            // Step 5: Remap DocumentChunks.DocumentVersionId from the old Document.Id
            // to the new DocumentVersion.Id we just created in Step 3.
            migrationBuilder.Sql(@"
                UPDATE ""DocumentChunks"" c
                SET ""DocumentVersionId"" = v.""Id""
                FROM ""DocumentVersions"" v
                WHERE c.""DocumentVersionId"" = v.""DocumentId"";
            ");

            // Step 6: Add the new FK from DocumentChunks -> DocumentVersions.
            migrationBuilder.AddForeignKey(
                name: "FK_DocumentChunks_DocumentVersions_DocumentVersionId",
                table: "DocumentChunks",
                column: "DocumentVersionId",
                principalTable: "DocumentVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // Step 7: Now safe to drop the old columns from Documents (data was migrated in Step 3).
            migrationBuilder.DropColumn(
                name: "ExtractedText",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "MinioObjectName",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Documents");

            // Step 8: Add new columns to Documents.
            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "Documents",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "Size",
                table: "Documents",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DocumentChunks_DocumentVersions_DocumentVersionId",
                table: "DocumentChunks");

            migrationBuilder.DropTable(
                name: "DocumentVersions");

            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "Size",
                table: "Documents");

            migrationBuilder.RenameColumn(
                name: "DocumentVersionId",
                table: "DocumentChunks",
                newName: "DocumentId");

            migrationBuilder.RenameIndex(
                name: "IX_DocumentChunks_TenantId_DocumentVersionId_ChunkIndex",
                table: "DocumentChunks",
                newName: "IX_DocumentChunks_TenantId_DocumentId_ChunkIndex");

            migrationBuilder.RenameIndex(
                name: "IX_DocumentChunks_DocumentVersionId",
                table: "DocumentChunks",
                newName: "IX_DocumentChunks_DocumentId");

            migrationBuilder.AddColumn<string>(
                name: "ExtractedText",
                table: "Documents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MinioObjectName",
                table: "Documents",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Documents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentChunks_Documents_DocumentId",
                table: "DocumentChunks",
                column: "DocumentId",
                principalTable: "Documents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
