using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domovoy.Database.Migrations
{
    /// <inheritdoc />
    public partial class SwitchToTPT : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Create Events table
            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Data_Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Data_Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Data_Value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Data_UserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Data_Metadata = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Events_Reports_Id",
                        column: x => x.Id,
                        principalTable: "Reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Step 2: Create Errors table
            migrationBuilder.CreateTable(
                name: "Errors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Data_Severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Data_Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Data_Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Data_ExceptionType = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Data_StackTrace = table.Column<string>(type: "text", nullable: true),
                    Data_Context = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Errors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Errors_Reports_Id",
                        column: x => x.Id,
                        principalTable: "Reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Step 3: Migrate existing Event data (ReportType = 1)
            migrationBuilder.Sql(@"
                INSERT INTO ""Events"" (
                    ""Id"", ""Data_Name"", ""Data_Category"", ""Data_Value"", ""Data_UserId"", ""Data_Metadata""
                )
                SELECT
                    ""Id"",
                    COALESCE(""Data_Name"", ''),
                    COALESCE(""Data_Category"", 'General'),
                    ""Data_Value"",
                    ""Data_UserId"",
                    ""Data_Metadata""
                FROM ""Reports""
                WHERE ""ReportType"" = 1;
            ");

            // Step 4: Migrate existing Error data (ReportType = 2)
            migrationBuilder.Sql(@"
                INSERT INTO ""Errors"" (
                    ""Id"", ""Data_Severity"", ""Data_Code"", ""Data_Message"",
                    ""Data_ExceptionType"", ""Data_StackTrace"", ""Data_Context""
                )
                SELECT
                    ""Id"",
                    COALESCE(""Data_Severity"", 'Error'),
                    ""Data_Code"",
                    COALESCE(""Data_Message"", ''),
                    ""Data_ExceptionType"",
                    ""Data_StackTrace"",
                    ""Data_Context""
                FROM ""Reports""
                WHERE ""ReportType"" = 2;
            ");

            // Step 5: Drop old indexes
            migrationBuilder.DropIndex(
                name: "IX_Reports_Data_Severity",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Reports_Data_UserId",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Reports_ReportType",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Reports_ReportType_Timestamp",
                table: "Reports");

            // Step 6: Drop type-specific columns from Reports table
            migrationBuilder.DropColumn(name: "Data_Category", table: "Reports");
            migrationBuilder.DropColumn(name: "Data_Code", table: "Reports");
            migrationBuilder.DropColumn(name: "Data_Context", table: "Reports");
            migrationBuilder.DropColumn(name: "Data_ExceptionType", table: "Reports");
            migrationBuilder.DropColumn(name: "Data_Message", table: "Reports");
            migrationBuilder.DropColumn(name: "Data_Metadata", table: "Reports");
            migrationBuilder.DropColumn(name: "Data_Name", table: "Reports");
            migrationBuilder.DropColumn(name: "Data_Severity", table: "Reports");
            migrationBuilder.DropColumn(name: "Data_StackTrace", table: "Reports");
            migrationBuilder.DropColumn(name: "Data_UserId", table: "Reports");
            migrationBuilder.DropColumn(name: "Data_Value", table: "Reports");

            // Step 7: Create indexes on new tables
            migrationBuilder.CreateIndex(
                name: "IX_Errors_Data_Severity",
                table: "Errors",
                column: "Data_Severity");

            migrationBuilder.CreateIndex(
                name: "IX_Events_Data_UserId",
                table: "Events",
                column: "Data_UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Errors");

            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.AddColumn<string>(
                name: "Data_Category",
                table: "Reports",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Data_Code",
                table: "Reports",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Data_Context",
                table: "Reports",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Data_ExceptionType",
                table: "Reports",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Data_Message",
                table: "Reports",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<Dictionary<string, object>>(
                name: "Data_Metadata",
                table: "Reports",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Data_Name",
                table: "Reports",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Data_Severity",
                table: "Reports",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Data_StackTrace",
                table: "Reports",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Data_UserId",
                table: "Reports",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Data_Value",
                table: "Reports",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

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
                name: "IX_Reports_ReportType",
                table: "Reports",
                column: "ReportType");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ReportType_Timestamp",
                table: "Reports",
                columns: new[] { "ReportType", "Timestamp" });
        }
    }
}
