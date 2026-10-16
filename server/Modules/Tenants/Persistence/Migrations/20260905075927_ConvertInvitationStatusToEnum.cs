using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Normora.Modules.Tenants.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConvertInvitationStatusToEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "TenantInvitations"
                ALTER COLUMN "Status" TYPE integer
                USING CASE LOWER("Status")
                    WHEN 'pending' THEN 0
                    WHEN 'accepted' THEN 1
                    WHEN 'revoked' THEN 2
                    ELSE 0
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "TenantInvitations"
                ALTER COLUMN "Status" TYPE character varying(20)
                USING CASE "Status"
                    WHEN 0 THEN 'Pending'
                    WHEN 1 THEN 'Accepted'
                    WHEN 2 THEN 'Revoked'
                    ELSE 'Pending'
                END;
                """);
        }
    }
}
