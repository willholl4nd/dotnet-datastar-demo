using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dotnet_html_sortable_table.Migrations
{
    /// <inheritdoc />
    public partial class Accounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpId = table.Column<int>(type: "INTEGER", nullable: false),
                    NamePrefix = table.Column<string>(type: "TEXT", nullable: false),
                    FirstName = table.Column<string>(type: "TEXT", nullable: false),
                    MiddleInitial = table.Column<string>(type: "TEXT", nullable: false),
                    LastName = table.Column<string>(type: "TEXT", nullable: false),
                    Gender = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    FathersName = table.Column<string>(type: "TEXT", nullable: false),
                    MothersName = table.Column<string>(type: "TEXT", nullable: false),
                    MothersMaidenName = table.Column<string>(type: "TEXT", nullable: false),
                    DateOfBirth = table.Column<string>(type: "TEXT", nullable: false),
                    TimeOfBirth = table.Column<string>(type: "TEXT", nullable: false),
                    AgeInYears = table.Column<double>(type: "REAL", nullable: false),
                    WeightInKgs = table.Column<int>(type: "INTEGER", nullable: false),
                    DateOfJoining = table.Column<string>(type: "TEXT", nullable: false),
                    QuarterOfJoining = table.Column<string>(type: "TEXT", nullable: false),
                    HalfOfJoining = table.Column<string>(type: "TEXT", nullable: false),
                    YearOfJoining = table.Column<int>(type: "INTEGER", nullable: false),
                    MonthOfJoining = table.Column<int>(type: "INTEGER", nullable: false),
                    MonthNameOfJoining = table.Column<string>(type: "TEXT", nullable: false),
                    ShortMonth = table.Column<string>(type: "TEXT", nullable: false),
                    DayOfJoining = table.Column<int>(type: "INTEGER", nullable: false),
                    DOWOfJoining = table.Column<string>(type: "TEXT", nullable: false),
                    ShortDOW = table.Column<string>(type: "TEXT", nullable: false),
                    AgeInCompanyYears = table.Column<double>(type: "REAL", nullable: false),
                    Salary = table.Column<int>(type: "INTEGER", nullable: false),
                    LastPercentHike = table.Column<string>(type: "TEXT", nullable: false),
                    SSN = table.Column<string>(type: "TEXT", nullable: false),
                    PhoneNumber = table.Column<string>(type: "TEXT", nullable: false),
                    PlaceName = table.Column<string>(type: "TEXT", nullable: false),
                    County = table.Column<string>(type: "TEXT", nullable: false),
                    City = table.Column<string>(type: "TEXT", nullable: false),
                    State = table.Column<string>(type: "TEXT", nullable: false),
                    Zip = table.Column<string>(type: "TEXT", nullable: false),
                    Region = table.Column<int>(type: "INTEGER", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: false),
                    Password = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Accounts");
        }
    }
}
