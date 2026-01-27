using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monolith.WebAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddJourneyStatusFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FailureReason",
                table: "JourneyStatuses",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhotoBase64",
                table: "JourneyStatuses",
                type: "nvarchar(max)",
                maxLength: 2147483647,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignatureBase64",
                table: "JourneyStatuses",
                type: "nvarchar(max)",
                maxLength: 2147483647,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FailureReason",
                table: "JourneyStatuses");

            migrationBuilder.DropColumn(
                name: "PhotoBase64",
                table: "JourneyStatuses");

            migrationBuilder.DropColumn(
                name: "SignatureBase64",
                table: "JourneyStatuses");
        }
    }
}
