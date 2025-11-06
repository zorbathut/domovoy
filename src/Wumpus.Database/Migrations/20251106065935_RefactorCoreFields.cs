using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wumpus.Database.Migrations
{
    /// <inheritdoc />
    public partial class RefactorCoreFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StackTrace",
                table: "CrashReports",
                newName: "Core_StackTrace");

            migrationBuilder.RenameColumn(
                name: "Platform",
                table: "CrashReports",
                newName: "Core_Platform");

            migrationBuilder.RenameColumn(
                name: "GameVersion",
                table: "CrashReports",
                newName: "Core_GameVersion");

            migrationBuilder.RenameColumn(
                name: "ExceptionType",
                table: "CrashReports",
                newName: "Core_ExceptionType");

            migrationBuilder.RenameColumn(
                name: "ExceptionMessage",
                table: "CrashReports",
                newName: "Core_ExceptionMessage");

            migrationBuilder.RenameIndex(
                name: "IX_CrashReports_Platform",
                table: "CrashReports",
                newName: "IX_CrashReports_Core_Platform");

            migrationBuilder.RenameIndex(
                name: "IX_CrashReports_GameVersion",
                table: "CrashReports",
                newName: "IX_CrashReports_Core_GameVersion");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Core_StackTrace",
                table: "CrashReports",
                newName: "StackTrace");

            migrationBuilder.RenameColumn(
                name: "Core_Platform",
                table: "CrashReports",
                newName: "Platform");

            migrationBuilder.RenameColumn(
                name: "Core_GameVersion",
                table: "CrashReports",
                newName: "GameVersion");

            migrationBuilder.RenameColumn(
                name: "Core_ExceptionType",
                table: "CrashReports",
                newName: "ExceptionType");

            migrationBuilder.RenameColumn(
                name: "Core_ExceptionMessage",
                table: "CrashReports",
                newName: "ExceptionMessage");

            migrationBuilder.RenameIndex(
                name: "IX_CrashReports_Core_Platform",
                table: "CrashReports",
                newName: "IX_CrashReports_Platform");

            migrationBuilder.RenameIndex(
                name: "IX_CrashReports_Core_GameVersion",
                table: "CrashReports",
                newName: "IX_CrashReports_GameVersion");
        }
    }
}
