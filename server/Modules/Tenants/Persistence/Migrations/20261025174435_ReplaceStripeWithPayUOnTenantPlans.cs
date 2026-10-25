using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Normora.Modules.Tenants.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceStripeWithPayUOnTenantPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StripeSubscriptionId",
                table: "TenantPlans",
                newName: "PayUTransactionId");

            migrationBuilder.RenameColumn(
                name: "StripeCustomerId",
                table: "TenantPlans",
                newName: "PayUPaymentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PayUTransactionId",
                table: "TenantPlans",
                newName: "StripeSubscriptionId");

            migrationBuilder.RenameColumn(
                name: "PayUPaymentId",
                table: "TenantPlans",
                newName: "StripeCustomerId");
        }
    }
}
