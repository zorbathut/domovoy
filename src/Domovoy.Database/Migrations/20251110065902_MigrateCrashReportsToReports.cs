using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domovoy.Database.Migrations
{
    /// <inheritdoc />
    public partial class MigrateCrashReportsToReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // First, create the new Reports table
            migrationBuilder.CreateTable(
                name: "Reports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReportType = table.Column<int>(type: "integer", nullable: false),
                    GameVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Platform = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Data_Severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Data_Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Data_Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Data_ExceptionType = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Data_StackTrace = table.Column<string>(type: "text", nullable: true),
                    Data_Context = table.Column<string>(type: "text", nullable: true),
                    Data_Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Data_Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Data_Value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Data_UserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Data_Metadata = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reports", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reports_Data_Severity",
                table: "Reports",
                column: "Data_Severity",
                filter: "\"ReportType\" = 2");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_Data_UserId",
                table: "Reports",
                column: "Data_UserId",
                filter: "\"ReportType\" = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_GameVersion",
                table: "Reports",
                column: "GameVersion");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_Platform",
                table: "Reports",
                column: "Platform");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ReportType",
                table: "Reports",
                column: "ReportType");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ReportType_Timestamp",
                table: "Reports",
                columns: new[] { "ReportType", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_Reports_Timestamp",
                table: "Reports",
                column: "Timestamp");

            // Migrate existing CrashReports data to Reports as Error type
            migrationBuilder.Sql(@"
                INSERT INTO ""Reports"" (
                    ""Id"", ""Timestamp"", ""ReportType"", ""GameVersion"", ""Platform"",
                    ""Data_Severity"", ""Data_Message"", ""Data_ExceptionType"", ""Data_StackTrace""
                )
                SELECT
                    ""Id"",
                    ""Timestamp"",
                    2 as ""ReportType"",  -- ReportType.Error = 2
                    ""Core_GameVersion"" as ""GameVersion"",
                    ""Core_Platform"" as ""Platform"",
                    'Fatal' as ""Data_Severity"",
                    ""Core_ExceptionMessage"" as ""Data_Message"",
                    ""Core_ExceptionType"" as ""Data_ExceptionType"",
                    ""Core_StackTrace"" as ""Data_StackTrace""
                FROM ""CrashReports"";
            ");

            // Drop the old CrashReports table
            migrationBuilder.DropTable(
                name: "CrashReports");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Reports");

            migrationBuilder.CreateTable(
                name: "CrashReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Core_ExceptionMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Core_ExceptionType = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Core_GameVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Core_Platform = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Core_StackTrace = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrashReports", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CrashReports_Core_GameVersion",
                table: "CrashReports",
                column: "Core_GameVersion");

            migrationBuilder.CreateIndex(
                name: "IX_CrashReports_Core_Platform",
                table: "CrashReports",
                column: "Core_Platform");

            migrationBuilder.CreateIndex(
                name: "IX_CrashReports_Timestamp",
                table: "CrashReports",
                column: "Timestamp");
        }
    }
}
