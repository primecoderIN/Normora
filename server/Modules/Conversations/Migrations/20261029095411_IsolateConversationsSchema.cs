using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Normora.Modules.Conversations.Migrations
{
    /// <inheritdoc />
    public partial class IsolateConversationsSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "conversations");

            migrationBuilder.RenameTable(
                name: "SavedAnswers",
                newName: "SavedAnswers",
                newSchema: "conversations");

            migrationBuilder.RenameTable(
                name: "Messages",
                newName: "Messages",
                newSchema: "conversations");

            migrationBuilder.RenameTable(
                name: "MessageFeedbacks",
                newName: "MessageFeedbacks",
                newSchema: "conversations");

            migrationBuilder.RenameTable(
                name: "MessageCitations",
                newName: "MessageCitations",
                newSchema: "conversations");

            migrationBuilder.RenameTable(
                name: "ConversationShares",
                newName: "ConversationShares",
                newSchema: "conversations");

            migrationBuilder.RenameTable(
                name: "Conversations",
                newName: "Conversations",
                newSchema: "conversations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "SavedAnswers",
                schema: "conversations",
                newName: "SavedAnswers");

            migrationBuilder.RenameTable(
                name: "Messages",
                schema: "conversations",
                newName: "Messages");

            migrationBuilder.RenameTable(
                name: "MessageFeedbacks",
                schema: "conversations",
                newName: "MessageFeedbacks");

            migrationBuilder.RenameTable(
                name: "MessageCitations",
                schema: "conversations",
                newName: "MessageCitations");

            migrationBuilder.RenameTable(
                name: "ConversationShares",
                schema: "conversations",
                newName: "ConversationShares");

            migrationBuilder.RenameTable(
                name: "Conversations",
                schema: "conversations",
                newName: "Conversations");
        }
    }
}
