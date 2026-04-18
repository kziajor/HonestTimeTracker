using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HonestTimeTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDefaultTaskPlannedHours : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "DefaultTaskPlannedHours",
                table: "Settings",
                type: "REAL",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Settings",
                keyColumn: "Id",
                keyValue: 1,
                column: "DefaultTaskPlannedHours",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultTaskPlannedHours",
                table: "Settings");
        }
    }
}
