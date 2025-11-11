using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domovoy.Database.Migrations
{
    /// <inheritdoc />
    public partial class UpdateErrorPayloadFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Data_Code",
                table: "Errors");

            migrationBuilder.DropColumn(
                name: "Data_Context",
                table: "Errors");

            migrationBuilder.DropColumn(
                name: "Data_ExceptionType",
                table: "Errors");

            migrationBuilder.AlterColumn<string>(
                name: "Data_StackTrace",
                table: "Errors",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Data_Log",
                table: "Errors",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Data_Log",
                table: "Errors");

            migrationBuilder.AlterColumn<string>(
                name: "Data_StackTrace",
                table: "Errors",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "Data_Code",
                table: "Errors",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Data_Context",
                table: "Errors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Data_ExceptionType",
                table: "Errors",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }
    }
}
