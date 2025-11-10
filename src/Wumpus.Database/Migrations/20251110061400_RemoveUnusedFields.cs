using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wumpus.Database.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUnusedFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstSeen",
                table: "CrashReports");

            migrationBuilder.DropColumn(
                name: "LastSeen",
                table: "CrashReports");

            migrationBuilder.DropColumn(
                name: "OccurrenceCount",
                table: "CrashReports");

            migrationBuilder.DropColumn(
                name: "SystemInfo",
                table: "CrashReports");

            migrationBuilder.DropColumn(
                name: "UserContext",
                table: "CrashReports");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FirstSeen",
                table: "CrashReports",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSeen",
                table: "CrashReports",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "OccurrenceCount",
                table: "CrashReports",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SystemInfo",
                table: "CrashReports",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserContext",
                table: "CrashReports",
                type: "jsonb",
                nullable: true);
        }
    }
}
