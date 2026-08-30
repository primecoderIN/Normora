using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Normora.Modules.Tenants.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantInvitations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Memberships_Tenants_TenantId",
                table: "Memberships");

            migrationBuilder.DropForeignKey(
                name: "FK_Memberships_Users_UserId",
                table: "Memberships");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Memberships",
                table: "Memberships");

            migrationBuilder.RenameTable(
                name: "Memberships",
                newName: "TenantMemberships");

            migrationBuilder.RenameIndex(
                name: "IX_Memberships_UserId",
                table: "TenantMemberships",
                newName: "IX_TenantMemberships_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Memberships_TenantId_UserId",
                table: "TenantMemberships",
                newName: "IX_TenantMemberships_TenantId_UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TenantMemberships",
                table: "TenantMemberships",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "TenantInvitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantInvitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantInvitations_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantInvitations_TenantId",
                table: "TenantInvitations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantInvitations_Token",
                table: "TenantInvitations",
                column: "Token",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TenantMemberships_Tenants_TenantId",
                table: "TenantMemberships",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TenantMemberships_Users_UserId",
                table: "TenantMemberships",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TenantMemberships_Tenants_TenantId",
                table: "TenantMemberships");

            migrationBuilder.DropForeignKey(
                name: "FK_TenantMemberships_Users_UserId",
                table: "TenantMemberships");

            migrationBuilder.DropTable(
                name: "TenantInvitations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TenantMemberships",
                table: "TenantMemberships");

            migrationBuilder.RenameTable(
                name: "TenantMemberships",
                newName: "Memberships");

            migrationBuilder.RenameIndex(
                name: "IX_TenantMemberships_UserId",
                table: "Memberships",
                newName: "IX_Memberships_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_TenantMemberships_TenantId_UserId",
                table: "Memberships",
                newName: "IX_Memberships_TenantId_UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Memberships",
                table: "Memberships",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Memberships_Tenants_TenantId",
                table: "Memberships",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Memberships_Users_UserId",
                table: "Memberships",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
