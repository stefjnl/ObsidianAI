using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ObsidianAI.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddThreadIdToConversation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ThreadId",
                table: "Conversations",
                type: "TEXT",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_ThreadId",
                table: "Conversations",
                column: "ThreadId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Conversations_ThreadId",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "ThreadId",
                table: "Conversations");
        }
    }
}
