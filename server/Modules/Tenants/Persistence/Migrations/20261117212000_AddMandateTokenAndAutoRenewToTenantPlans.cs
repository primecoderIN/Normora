using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Normora.Modules.Tenants.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMandateTokenAndAutoRenewToTenantPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MandateToken",
                schema: "tenants",
                table: "TenantPlans",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AutoRenewEnabled",
                schema: "tenants",
                table: "TenantPlans",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MandateToken",
                schema: "tenants",
                table: "TenantPlans");

            migrationBuilder.DropColumn(
                name: "AutoRenewEnabled",
                schema: "tenants",
                table: "TenantPlans");
        }
    }
}
