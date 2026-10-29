using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Normora.Modules.Tenants.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IsolateTenantsSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "tenants");

            migrationBuilder.RenameTable(
                name: "Users",
                newName: "Users",
                newSchema: "tenants");

            migrationBuilder.RenameTable(
                name: "UserGroups",
                newName: "UserGroups",
                newSchema: "tenants");

            migrationBuilder.RenameTable(
                name: "UserGroupMemberships",
                newName: "UserGroupMemberships",
                newSchema: "tenants");

            migrationBuilder.RenameTable(
                name: "UserGroupDepartments",
                newName: "UserGroupDepartments",
                newSchema: "tenants");

            migrationBuilder.RenameTable(
                name: "Tenants",
                newName: "Tenants",
                newSchema: "tenants");

            migrationBuilder.RenameTable(
                name: "TenantPlans",
                newName: "TenantPlans",
                newSchema: "tenants");

            migrationBuilder.RenameTable(
                name: "TenantMemberships",
                newName: "TenantMemberships",
                newSchema: "tenants");

            migrationBuilder.RenameTable(
                name: "TenantInvitations",
                newName: "TenantInvitations",
                newSchema: "tenants");

            migrationBuilder.RenameTable(
                name: "TenantBrandings",
                newName: "TenantBrandings",
                newSchema: "tenants");

            migrationBuilder.RenameTable(
                name: "MembershipDepartments",
                newName: "MembershipDepartments",
                newSchema: "tenants");

            migrationBuilder.RenameTable(
                name: "Departments",
                newName: "Departments",
                newSchema: "tenants");

            migrationBuilder.RenameTable(
                name: "AuditLogs",
                newName: "AuditLogs",
                newSchema: "tenants");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "Users",
                schema: "tenants",
                newName: "Users");

            migrationBuilder.RenameTable(
                name: "UserGroups",
                schema: "tenants",
                newName: "UserGroups");

            migrationBuilder.RenameTable(
                name: "UserGroupMemberships",
                schema: "tenants",
                newName: "UserGroupMemberships");

            migrationBuilder.RenameTable(
                name: "UserGroupDepartments",
                schema: "tenants",
                newName: "UserGroupDepartments");

            migrationBuilder.RenameTable(
                name: "Tenants",
                schema: "tenants",
                newName: "Tenants");

            migrationBuilder.RenameTable(
                name: "TenantPlans",
                schema: "tenants",
                newName: "TenantPlans");

            migrationBuilder.RenameTable(
                name: "TenantMemberships",
                schema: "tenants",
                newName: "TenantMemberships");

            migrationBuilder.RenameTable(
                name: "TenantInvitations",
                schema: "tenants",
                newName: "TenantInvitations");

            migrationBuilder.RenameTable(
                name: "TenantBrandings",
                schema: "tenants",
                newName: "TenantBrandings");

            migrationBuilder.RenameTable(
                name: "MembershipDepartments",
                schema: "tenants",
                newName: "MembershipDepartments");

            migrationBuilder.RenameTable(
                name: "Departments",
                schema: "tenants",
                newName: "Departments");

            migrationBuilder.RenameTable(
                name: "AuditLogs",
                schema: "tenants",
                newName: "AuditLogs");
        }
    }
}
