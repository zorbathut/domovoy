using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domovoy.Database.Migrations
{
    /// <inheritdoc />
    public partial class RevampEventPayload : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Events_Data_UserId",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "Data_UserId",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "Data_Value",
                table: "Events");

            migrationBuilder.AddColumn<Dictionary<string, object>>(
                name: "Data_Data",
                table: "Events",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Data_Data",
                table: "Events");

            migrationBuilder.AddColumn<string>(
                name: "Data_UserId",
                table: "Events",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Data_Value",
                table: "Events",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Events_Data_UserId",
                table: "Events",
                column: "Data_UserId");
        }
    }
}
