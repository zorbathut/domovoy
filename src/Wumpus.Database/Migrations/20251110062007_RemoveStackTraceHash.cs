using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wumpus.Database.Migrations
{
    /// <inheritdoc />
    public partial class RemoveStackTraceHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CrashReports_StackTraceHash",
                table: "CrashReports");

            migrationBuilder.DropColumn(
                name: "StackTraceHash",
                table: "CrashReports");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StackTraceHash",
                table: "CrashReports",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_CrashReports_StackTraceHash",
                table: "CrashReports",
                column: "StackTraceHash");
        }
    }
}
