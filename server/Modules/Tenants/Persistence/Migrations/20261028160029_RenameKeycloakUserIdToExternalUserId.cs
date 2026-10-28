using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Normora.Modules.Tenants.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameKeycloakUserIdToExternalUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "KeycloakUserId",
                table: "Users",
                newName: "ExternalUserId");

            migrationBuilder.RenameIndex(
                name: "IX_Users_KeycloakUserId",
                table: "Users",
                newName: "IX_Users_ExternalUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ExternalUserId",
                table: "Users",
                newName: "KeycloakUserId");

            migrationBuilder.RenameIndex(
                name: "IX_Users_ExternalUserId",
                table: "Users",
                newName: "IX_Users_KeycloakUserId");
        }
    }
}
