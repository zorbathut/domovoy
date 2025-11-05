using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wumpus.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CrashReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GameVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Platform = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ExceptionType = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ExceptionMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    StackTrace = table.Column<string>(type: "text", nullable: false),
                    StackTraceHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OccurrenceCount = table.Column<int>(type: "integer", nullable: false),
                    FirstSeen = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSeen = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SystemInfo = table.Column<string>(type: "jsonb", nullable: true),
                    UserContext = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrashReports", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CrashReports_GameVersion",
                table: "CrashReports",
                column: "GameVersion");

            migrationBuilder.CreateIndex(
                name: "IX_CrashReports_Platform",
                table: "CrashReports",
                column: "Platform");

            migrationBuilder.CreateIndex(
                name: "IX_CrashReports_StackTraceHash",
                table: "CrashReports",
                column: "StackTraceHash");

            migrationBuilder.CreateIndex(
                name: "IX_CrashReports_Timestamp",
                table: "CrashReports",
                column: "Timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CrashReports");
        }
    }
}
