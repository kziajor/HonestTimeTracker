using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HonestTimeTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixCascadeToRestrict : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Records_Tasks_TaskId",
                table: "Records");

            migrationBuilder.AddForeignKey(
                name: "FK_Records_Tasks_TaskId",
                table: "Records",
                column: "TaskId",
                principalTable: "Tasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Records_Tasks_TaskId",
                table: "Records");

            migrationBuilder.AddForeignKey(
                name: "FK_Records_Tasks_TaskId",
                table: "Records",
                column: "TaskId",
                principalTable: "Tasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
