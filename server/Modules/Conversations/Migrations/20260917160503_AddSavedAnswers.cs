using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Normora.Modules.Conversations.Migrations
{
    /// <inheritdoc />
    public partial class AddSavedAnswers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SavedAnswers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavedAnswers_Messages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "Messages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SavedAnswers_ConversationId",
                table: "SavedAnswers",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedAnswers_MessageId",
                table: "SavedAnswers",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedAnswers_UserId",
                table: "SavedAnswers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedAnswers_UserId_MessageId",
                table: "SavedAnswers",
                columns: new[] { "UserId", "MessageId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SavedAnswers");
        }
    }
}
