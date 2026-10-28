using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Normora.Modules.Tenants.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenamePayUColumnsToPaymentColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PayUTransactionId",
                table: "TenantPlans",
                newName: "PaymentTransactionId");

            migrationBuilder.RenameColumn(
                name: "PayUPaymentId",
                table: "TenantPlans",
                newName: "PaymentReferenceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PaymentTransactionId",
                table: "TenantPlans",
                newName: "PayUTransactionId");

            migrationBuilder.RenameColumn(
                name: "PaymentReferenceId",
                table: "TenantPlans",
                newName: "PayUPaymentId");
        }
    }
}
