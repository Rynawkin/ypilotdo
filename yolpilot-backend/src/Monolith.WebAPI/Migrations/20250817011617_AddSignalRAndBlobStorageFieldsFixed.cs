using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monolith.WebAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddSignalRAndBlobStorageFieldsFixed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Journeys_Vehicles_VehicleId",
                table: "Journeys");

            migrationBuilder.AddColumn<DateTime>(
                name: "CheckInTime",
                table: "JourneyStops",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CheckOutTime",
                table: "JourneyStops",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "JourneyStops",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "JourneyStops",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "JourneyStops",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhotoUrl",
                table: "JourneyStatuses",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignatureUrl",
                table: "JourneyStatuses",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "Journeys",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrentStopIndex",
                table: "Journeys",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "LiveLocation_Accuracy",
                table: "Journeys",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LiveLocation_Heading",
                table: "Journeys",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LiveLocation_Latitude",
                table: "Journeys",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LiveLocation_Longitude",
                table: "Journeys",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LiveLocation_Speed",
                table: "Journeys",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LiveLocation_Timestamp",
                table: "Journeys",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Journeys",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WorkspaceId",
                table: "Journeys",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Journeys_WorkspaceId",
                table: "Journeys",
                column: "WorkspaceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Journeys_Vehicles_VehicleId",
                table: "Journeys",
                column: "VehicleId",
                principalTable: "Vehicles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Journeys_Workspaces_WorkspaceId",
                table: "Journeys",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Journeys_Vehicles_VehicleId",
                table: "Journeys");

            migrationBuilder.DropForeignKey(
                name: "FK_Journeys_Workspaces_WorkspaceId",
                table: "Journeys");

            migrationBuilder.DropIndex(
                name: "IX_Journeys_WorkspaceId",
                table: "Journeys");

            migrationBuilder.DropColumn(
                name: "CheckInTime",
                table: "JourneyStops");

            migrationBuilder.DropColumn(
                name: "CheckOutTime",
                table: "JourneyStops");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "JourneyStops");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "JourneyStops");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "JourneyStops");

            migrationBuilder.DropColumn(
                name: "PhotoUrl",
                table: "JourneyStatuses");

            migrationBuilder.DropColumn(
                name: "SignatureUrl",
                table: "JourneyStatuses");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "Journeys");

            migrationBuilder.DropColumn(
                name: "CurrentStopIndex",
                table: "Journeys");

            migrationBuilder.DropColumn(
                name: "LiveLocation_Accuracy",
                table: "Journeys");

            migrationBuilder.DropColumn(
                name: "LiveLocation_Heading",
                table: "Journeys");

            migrationBuilder.DropColumn(
                name: "LiveLocation_Latitude",
                table: "Journeys");

            migrationBuilder.DropColumn(
                name: "LiveLocation_Longitude",
                table: "Journeys");

            migrationBuilder.DropColumn(
                name: "LiveLocation_Speed",
                table: "Journeys");

            migrationBuilder.DropColumn(
                name: "LiveLocation_Timestamp",
                table: "Journeys");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Journeys");

            migrationBuilder.DropColumn(
                name: "WorkspaceId",
                table: "Journeys");

            migrationBuilder.AddForeignKey(
                name: "FK_Journeys_Vehicles_VehicleId",
                table: "Journeys",
                column: "VehicleId",
                principalTable: "Vehicles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
