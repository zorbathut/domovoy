using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wumpus.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddEnvironmentField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Standard_Environment",
                table: "Reports",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_Standard_Environment",
                table: "Reports",
                column: "Standard_Environment");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reports_Standard_Environment",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "Standard_Environment",
                table: "Reports");
        }
    }
}
