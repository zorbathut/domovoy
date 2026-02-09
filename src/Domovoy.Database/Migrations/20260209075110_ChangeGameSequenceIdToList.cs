using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domovoy.Database.Migrations
{
    /// <inheritdoc />
    public partial class ChangeGameSequenceIdToList : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<Guid>>(
                name: "Standard_GameSequenceIds",
                table: "Reports",
                type: "uuid[]",
                nullable: false,
                defaultValue: new List<Guid>());

            migrationBuilder.Sql(
                """UPDATE "Reports" SET "Standard_GameSequenceIds" = ARRAY["Standard_GameSequenceId"]""");

            migrationBuilder.DropColumn(
                name: "Standard_GameSequenceId",
                table: "Reports");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "Standard_GameSequenceId",
                table: "Reports",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql(
                """UPDATE "Reports" SET "Standard_GameSequenceId" = "Standard_GameSequenceIds"[1]""");

            migrationBuilder.DropColumn(
                name: "Standard_GameSequenceIds",
                table: "Reports");
        }
    }
}
