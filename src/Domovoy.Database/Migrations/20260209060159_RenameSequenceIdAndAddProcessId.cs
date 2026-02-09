using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domovoy.Database.Migrations
{
    /// <inheritdoc />
    public partial class RenameSequenceIdAndAddProcessId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Standard_SequenceId",
                table: "Reports",
                newName: "Standard_GameSequenceId");

            migrationBuilder.AddColumn<Guid>(
                name: "Standard_ProcessId",
                table: "Reports",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Standard_ProcessId",
                table: "Reports");

            migrationBuilder.RenameColumn(
                name: "Standard_GameSequenceId",
                table: "Reports",
                newName: "Standard_SequenceId");
        }
    }
}
