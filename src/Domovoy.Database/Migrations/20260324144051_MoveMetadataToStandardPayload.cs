using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domovoy.Database.Migrations
{
    /// <inheritdoc />
    public partial class MoveMetadataToStandardPayload : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Dictionary<string, object>>(
                name: "Standard_Metadata",
                table: "Reports",
                type: "jsonb",
                nullable: true);

            // Migrate existing metadata from Events to Reports
            migrationBuilder.Sql(
                """
                UPDATE "Reports" r
                SET "Standard_Metadata" = e."Data_Metadata"
                FROM "Events" e
                WHERE r."Id" = e."Id" AND e."Data_Metadata" IS NOT NULL
                """);

            migrationBuilder.DropColumn(
                name: "Data_Metadata",
                table: "Events");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Standard_Metadata",
                table: "Reports");

            migrationBuilder.AddColumn<Dictionary<string, object>>(
                name: "Data_Metadata",
                table: "Events",
                type: "jsonb",
                nullable: true);
        }
    }
}
