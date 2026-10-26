using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HonestTimeTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTfsSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Enabled",
                table: "TfsCollections",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "EncryptedPat",
                table: "TfsCollections",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TfsCollectionId",
                table: "Tasks",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "VerboseTfsLogging",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "TfsImportPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TfsCollectionId = table.Column<int>(type: "INTEGER", nullable: false),
                    TfsProjectName = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                    LastProjectId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TfsImportPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TfsImportPreferences_TfsCollections_TfsCollectionId",
                        column: x => x.TfsCollectionId,
                        principalTable: "TfsCollections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "Settings",
                keyColumn: "Id",
                keyValue: 1,
                column: "VerboseTfsLogging",
                value: false);

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_TfsCollectionId",
                table: "Tasks",
                column: "TfsCollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_TfsImportPreferences_TfsCollectionId_TfsProjectName",
                table: "TfsImportPreferences",
                columns: new[] { "TfsCollectionId", "TfsProjectName" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_TfsCollections_TfsCollectionId",
                table: "Tasks",
                column: "TfsCollectionId",
                principalTable: "TfsCollections",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_TfsCollections_TfsCollectionId",
                table: "Tasks");

            migrationBuilder.DropTable(
                name: "TfsImportPreferences");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_TfsCollectionId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "Enabled",
                table: "TfsCollections");

            migrationBuilder.DropColumn(
                name: "EncryptedPat",
                table: "TfsCollections");

            migrationBuilder.DropColumn(
                name: "TfsCollectionId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "VerboseTfsLogging",
                table: "Settings");
        }
    }
}
