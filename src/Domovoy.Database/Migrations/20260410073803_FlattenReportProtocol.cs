using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Domovoy.Database.Migrations
{
    /// <inheritdoc />
    public partial class FlattenReportProtocol : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Standard_UserId",
                table: "Reports",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "Standard_ProcessId",
                table: "Reports",
                newName: "ProcessId");

            migrationBuilder.RenameColumn(
                name: "Standard_Platform",
                table: "Reports",
                newName: "Platform");

            migrationBuilder.RenameColumn(
                name: "Standard_Metadata",
                table: "Reports",
                newName: "Metadata");

            migrationBuilder.RenameColumn(
                name: "Standard_Environment",
                table: "Reports",
                newName: "Environment");

            migrationBuilder.RenameColumn(
                name: "Standard_ComputerId",
                table: "Reports",
                newName: "ComputerId");

            migrationBuilder.RenameColumn(
                name: "Standard_GameVersion",
                table: "Reports",
                newName: "Version");

            migrationBuilder.RenameColumn(
                name: "Standard_GameSequenceIds",
                table: "Reports",
                newName: "CampaignSequenceIds");

            migrationBuilder.RenameColumn(
                name: "Standard_GameId",
                table: "Reports",
                newName: "CampaignId");

            migrationBuilder.RenameIndex(
                name: "IX_Reports_Standard_UserId",
                table: "Reports",
                newName: "IX_Reports_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Reports_Standard_Platform",
                table: "Reports",
                newName: "IX_Reports_Platform");

            migrationBuilder.RenameIndex(
                name: "IX_Reports_Standard_GameVersion",
                table: "Reports",
                newName: "IX_Reports_Version");

            migrationBuilder.RenameIndex(
                name: "IX_Reports_Standard_GameId",
                table: "Reports",
                newName: "IX_Reports_CampaignId");

            migrationBuilder.RenameIndex(
                name: "IX_Reports_Standard_Environment",
                table: "Reports",
                newName: "IX_Reports_Environment");

            migrationBuilder.RenameIndex(
                name: "IX_Reports_Standard_ComputerId",
                table: "Reports",
                newName: "IX_Reports_ComputerId");

            migrationBuilder.RenameColumn(
                name: "Data_Name",
                table: "Events",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "Data_Data",
                table: "Events",
                newName: "Data");

            migrationBuilder.RenameColumn(
                name: "Data_Category",
                table: "Events",
                newName: "Category");

            migrationBuilder.RenameColumn(
                name: "Data_StackTrace",
                table: "Errors",
                newName: "StackTrace");

            migrationBuilder.RenameColumn(
                name: "Data_Severity",
                table: "Errors",
                newName: "Severity");

            migrationBuilder.RenameColumn(
                name: "Data_Message",
                table: "Errors",
                newName: "Message");

            migrationBuilder.RenameColumn(
                name: "Data_Log",
                table: "Errors",
                newName: "Log");

            migrationBuilder.RenameIndex(
                name: "IX_Errors_Data_Severity",
                table: "Errors",
                newName: "IX_Errors_Severity");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Reports",
                newName: "Standard_UserId");

            migrationBuilder.RenameColumn(
                name: "ProcessId",
                table: "Reports",
                newName: "Standard_ProcessId");

            migrationBuilder.RenameColumn(
                name: "Platform",
                table: "Reports",
                newName: "Standard_Platform");

            migrationBuilder.RenameColumn(
                name: "Metadata",
                table: "Reports",
                newName: "Standard_Metadata");

            migrationBuilder.RenameColumn(
                name: "Environment",
                table: "Reports",
                newName: "Standard_Environment");

            migrationBuilder.RenameColumn(
                name: "ComputerId",
                table: "Reports",
                newName: "Standard_ComputerId");

            migrationBuilder.RenameColumn(
                name: "Version",
                table: "Reports",
                newName: "Standard_GameVersion");

            migrationBuilder.RenameColumn(
                name: "CampaignSequenceIds",
                table: "Reports",
                newName: "Standard_GameSequenceIds");

            migrationBuilder.RenameColumn(
                name: "CampaignId",
                table: "Reports",
                newName: "Standard_GameId");

            migrationBuilder.RenameIndex(
                name: "IX_Reports_Version",
                table: "Reports",
                newName: "IX_Reports_Standard_GameVersion");

            migrationBuilder.RenameIndex(
                name: "IX_Reports_UserId",
                table: "Reports",
                newName: "IX_Reports_Standard_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Reports_Platform",
                table: "Reports",
                newName: "IX_Reports_Standard_Platform");

            migrationBuilder.RenameIndex(
                name: "IX_Reports_Environment",
                table: "Reports",
                newName: "IX_Reports_Standard_Environment");

            migrationBuilder.RenameIndex(
                name: "IX_Reports_ComputerId",
                table: "Reports",
                newName: "IX_Reports_Standard_ComputerId");

            migrationBuilder.RenameIndex(
                name: "IX_Reports_CampaignId",
                table: "Reports",
                newName: "IX_Reports_Standard_GameId");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Events",
                newName: "Data_Name");

            migrationBuilder.RenameColumn(
                name: "Data",
                table: "Events",
                newName: "Data_Data");

            migrationBuilder.RenameColumn(
                name: "Category",
                table: "Events",
                newName: "Data_Category");

            migrationBuilder.RenameColumn(
                name: "StackTrace",
                table: "Errors",
                newName: "Data_StackTrace");

            migrationBuilder.RenameColumn(
                name: "Severity",
                table: "Errors",
                newName: "Data_Severity");

            migrationBuilder.RenameColumn(
                name: "Message",
                table: "Errors",
                newName: "Data_Message");

            migrationBuilder.RenameColumn(
                name: "Log",
                table: "Errors",
                newName: "Data_Log");

            migrationBuilder.RenameIndex(
                name: "IX_Errors_Severity",
                table: "Errors",
                newName: "IX_Errors_Data_Severity");
        }
    }
}
