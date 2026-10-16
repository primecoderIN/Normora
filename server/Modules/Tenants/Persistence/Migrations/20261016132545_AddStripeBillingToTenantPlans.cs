using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Normora.Modules.Tenants.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStripeBillingToTenantPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TenantMemberships_TenantId_UserId",
                table: "TenantMemberships");

            migrationBuilder.AddColumn<bool>(
                name: "CancelAtPeriodEnd",
                table: "TenantPlans",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "CurrentPeriodEndUtc",
                table: "TenantPlans",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeCustomerId",
                table: "TenantPlans",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeSubscriptionId",
                table: "TenantPlans",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubscriptionStatus",
                table: "TenantPlans",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantMemberships_TenantId_UserId",
                table: "TenantMemberships",
                columns: new[] { "TenantId", "UserId" },
                unique: true,
                filter: "\"RemovedAt\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TenantMemberships_TenantId_UserId",
                table: "TenantMemberships");

            migrationBuilder.DropColumn(
                name: "CancelAtPeriodEnd",
                table: "TenantPlans");

            migrationBuilder.DropColumn(
                name: "CurrentPeriodEndUtc",
                table: "TenantPlans");

            migrationBuilder.DropColumn(
                name: "StripeCustomerId",
                table: "TenantPlans");

            migrationBuilder.DropColumn(
                name: "StripeSubscriptionId",
                table: "TenantPlans");

            migrationBuilder.DropColumn(
                name: "SubscriptionStatus",
                table: "TenantPlans");

            migrationBuilder.CreateIndex(
                name: "IX_TenantMemberships_TenantId_UserId",
                table: "TenantMemberships",
                columns: new[] { "TenantId", "UserId" },
                unique: true);
        }
    }
}
