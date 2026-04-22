using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HonestTimeTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskClosedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ClosedAt",
                table: "Tasks",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClosedAt",
                table: "Tasks");
        }
    }
}
