using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace dotnet_html_sortable_table.Migrations
{
    /// <inheritdoc />
    public partial class init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TableContainer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IdSort = table.Column<bool>(type: "INTEGER", nullable: false),
                    RandIntSort = table.Column<bool>(type: "INTEGER", nullable: false),
                    NameSort = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TableContainer", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Entries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RandInt = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    DemoObjectId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Entries_TableContainer_DemoObjectId",
                        column: x => x.DemoObjectId,
                        principalTable: "TableContainer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "TableContainer",
                columns: new[] { "Id", "IdSort", "NameSort", "RandIntSort" },
                values: new object[] { 1, false, false, false });

            migrationBuilder.InsertData(
                table: "Entries",
                columns: new[] { "Id", "DemoObjectId", "Name", "RandInt" },
                values: new object[,]
                {
                    { 1, 1, "Bill", 3 },
                    { 2, 1, "Bob", 9 },
                    { 3, 1, "Jim", 0 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Entries_DemoObjectId",
                table: "Entries",
                column: "DemoObjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Entries");

            migrationBuilder.DropTable(
                name: "TableContainer");
        }
    }
}
