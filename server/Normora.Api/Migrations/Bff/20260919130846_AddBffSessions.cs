using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Normora.Api.Migrations.Bff
{
    /// <inheritdoc />
    public partial class AddBffSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserSessions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SubjectId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SessionId = table.Column<string>(type: "text", nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Renewed = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Expires = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ticket = table.Column<string>(type: "text", nullable: false),
                    Key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PartitionKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSessions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_Expires",
                table: "UserSessions",
                column: "Expires");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_PartitionKey_Key",
                table: "UserSessions",
                columns: new[] { "PartitionKey", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_PartitionKey_SessionId",
                table: "UserSessions",
                columns: new[] { "PartitionKey", "SessionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_PartitionKey_SubjectId_SessionId",
                table: "UserSessions",
                columns: new[] { "PartitionKey", "SubjectId", "SessionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserSessions");
        }
    }
}
