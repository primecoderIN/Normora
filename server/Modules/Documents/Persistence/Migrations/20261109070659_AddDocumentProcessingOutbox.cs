using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Normora.Modules.Documents.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentProcessingOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DocumentProcessingOutboxMessages",
                schema: "documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentProcessingOutboxMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentProcessingOutboxMessages_DocumentVersions_DocumentV~",
                        column: x => x.DocumentVersionId,
                        principalSchema: "documents",
                        principalTable: "DocumentVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentProcessingOutboxMessages_CreatedAt",
                schema: "documents",
                table: "DocumentProcessingOutboxMessages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentProcessingOutboxMessages_DocumentVersionId",
                schema: "documents",
                table: "DocumentProcessingOutboxMessages",
                column: "DocumentVersionId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentProcessingOutboxMessages",
                schema: "documents");
        }
    }
}
