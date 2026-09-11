using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace Normora.Modules.Documents.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentSearchVector : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "DocumentChunks",
                type: "tsvector",
                nullable: true)
                .Annotation("Npgsql:TsVectorConfig", "english")
                .Annotation("Npgsql:TsVectorProperties", new[] { "Content" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentChunks_SearchVector",
                table: "DocumentChunks",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DocumentChunks_SearchVector",
                table: "DocumentChunks");

            migrationBuilder.DropColumn(
                name: "SearchVector",
                table: "DocumentChunks");
        }
    }
}
