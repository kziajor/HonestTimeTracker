using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HonestTimeTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFloatingTimerSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ShowFloatingTimer",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<double>(
                name: "FloatingTimerLeft",
                table: "Settings",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "FloatingTimerTop",
                table: "Settings",
                type: "REAL",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Settings",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ShowFloatingTimer", "FloatingTimerLeft", "FloatingTimerTop" },
                values: new object[] { true, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ShowFloatingTimer", table: "Settings");
            migrationBuilder.DropColumn(name: "FloatingTimerLeft", table: "Settings");
            migrationBuilder.DropColumn(name: "FloatingTimerTop", table: "Settings");
        }
    }
}
