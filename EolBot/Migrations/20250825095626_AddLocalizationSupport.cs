using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EolBot.Migrations
{
    /// <inheritdoc />
    public partial class AddLocalizationSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Data",
                table: "Reports");

            migrationBuilder.AddColumn<string>(
                name: "LanguageCode",
                table: "Users",
                type: "TEXT",
                maxLength: 5,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "From",
                table: "Reports",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "To",
                table: "Reports",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "ReportContent",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProductName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    ProductVersion = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    ProductUrl = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    Eol = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ReportId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportContent", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportContent_Reports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "Reports",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReportContent_ReportId",
                table: "ReportContent",
                column: "ReportId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReportContent");

            migrationBuilder.DropColumn(
                name: "LanguageCode",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "From",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "To",
                table: "Reports");

            migrationBuilder.AddColumn<string>(
                name: "Data",
                table: "Reports",
                type: "TEXT",
                maxLength: 4096,
                nullable: false,
                defaultValue: "");
        }
    }
}
