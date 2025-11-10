using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wumpus.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddStandardPayload : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Platform",
                table: "Reports",
                newName: "Standard_Platform");

            migrationBuilder.RenameColumn(
                name: "GameVersion",
                table: "Reports",
                newName: "Standard_GameVersion");

            migrationBuilder.RenameIndex(
                name: "IX_Reports_Platform",
                table: "Reports",
                newName: "IX_Reports_Standard_Platform");

            migrationBuilder.RenameIndex(
                name: "IX_Reports_GameVersion",
                table: "Reports",
                newName: "IX_Reports_Standard_GameVersion");

            migrationBuilder.AddColumn<Guid>(
                name: "Standard_ComputerId",
                table: "Reports",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "Standard_GameId",
                table: "Reports",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "Standard_SequenceId",
                table: "Reports",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "Standard_UserId",
                table: "Reports",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Reports_Standard_ComputerId",
                table: "Reports",
                column: "Standard_ComputerId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_Standard_GameId",
                table: "Reports",
                column: "Standard_GameId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_Standard_UserId",
                table: "Reports",
                column: "Standard_UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reports_Standard_ComputerId",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Reports_Standard_GameId",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Reports_Standard_UserId",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "Standard_ComputerId",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "Standard_GameId",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "Standard_SequenceId",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "Standard_UserId",
                table: "Reports");

            migrationBuilder.RenameColumn(
                name: "Standard_Platform",
                table: "Reports",
                newName: "Platform");

            migrationBuilder.RenameColumn(
                name: "Standard_GameVersion",
                table: "Reports",
                newName: "GameVersion");

            migrationBuilder.RenameIndex(
                name: "IX_Reports_Standard_Platform",
                table: "Reports",
                newName: "IX_Reports_Platform");

            migrationBuilder.RenameIndex(
                name: "IX_Reports_Standard_GameVersion",
                table: "Reports",
                newName: "IX_Reports_GameVersion");
        }
    }
}
