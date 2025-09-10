using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dotnet_html_sortable_table.Migrations.Messages
{
    /// <inheritdoc />
    public partial class Chatroomkeysfordistinctchatrooms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ChatRoomKey",
                table: "Messages",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("UPDATE Messages SET ChatRoomKey = 'Test1234'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChatRoomKey",
                table: "Messages");
        }
    }
}
