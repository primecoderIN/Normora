using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Normora.Modules.Documents.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentProcessingStates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "Documents"
                SET "Status" = CASE "Status"
                    WHEN 0 THEN 1
                    WHEN 1 THEN 2
                    WHEN 2 THEN 3
                    ELSE "Status"
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "Documents"
                SET "Status" = CASE "Status"
                    WHEN 1 THEN 0
                    WHEN 2 THEN 1
                    WHEN 3 THEN 2
                    ELSE "Status"
                END;
                """);
        }
    }
}
