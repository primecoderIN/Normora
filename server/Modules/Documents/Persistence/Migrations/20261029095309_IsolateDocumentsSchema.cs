using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Normora.Modules.Documents.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IsolateDocumentsSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "documents");

            migrationBuilder.RenameTable(
                name: "DocumentVersions",
                newName: "DocumentVersions",
                newSchema: "documents");

            migrationBuilder.RenameTable(
                name: "Documents",
                newName: "Documents",
                newSchema: "documents");

            migrationBuilder.RenameTable(
                name: "DocumentDepartments",
                newName: "DocumentDepartments",
                newSchema: "documents");

            migrationBuilder.RenameTable(
                name: "DocumentChunks",
                newName: "DocumentChunks",
                newSchema: "documents");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "DocumentVersions",
                schema: "documents",
                newName: "DocumentVersions");

            migrationBuilder.RenameTable(
                name: "Documents",
                schema: "documents",
                newName: "Documents");

            migrationBuilder.RenameTable(
                name: "DocumentDepartments",
                schema: "documents",
                newName: "DocumentDepartments");

            migrationBuilder.RenameTable(
                name: "DocumentChunks",
                schema: "documents",
                newName: "DocumentChunks");
        }
    }
}
