using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Normora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDocumentToUseTenantId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Documents_EmployerId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "EmployerId",
                table: "Documents");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Documents",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Documents_TenantId",
                table: "Documents",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Documents_TenantId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Documents");

            migrationBuilder.AddColumn<string>(
                name: "EmployerId",
                table: "Documents",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_EmployerId",
                table: "Documents",
                column: "EmployerId");
        }
    }
}
