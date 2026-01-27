using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monolith.WebAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CurrentMonthAdditionalCharges",
                table: "Workspaces",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "CurrentMonthStops",
                table: "Workspaces",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CurrentMonthWhatsAppMessages",
                table: "Workspaces",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastStopResetDate",
                table: "Workspaces",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "LastWhatsAppResetDate",
                table: "Workspaces",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "PlanEndDate",
                table: "Workspaces",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PlanStartDate",
                table: "Workspaces",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PlanType",
                table: "Workspaces",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ExclusionReason",
                table: "RouteStops",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsExcluded",
                table: "RouteStops",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentMonthAdditionalCharges",
                table: "Workspaces");

            migrationBuilder.DropColumn(
                name: "CurrentMonthStops",
                table: "Workspaces");

            migrationBuilder.DropColumn(
                name: "CurrentMonthWhatsAppMessages",
                table: "Workspaces");

            migrationBuilder.DropColumn(
                name: "LastStopResetDate",
                table: "Workspaces");

            migrationBuilder.DropColumn(
                name: "LastWhatsAppResetDate",
                table: "Workspaces");

            migrationBuilder.DropColumn(
                name: "PlanEndDate",
                table: "Workspaces");

            migrationBuilder.DropColumn(
                name: "PlanStartDate",
                table: "Workspaces");

            migrationBuilder.DropColumn(
                name: "PlanType",
                table: "Workspaces");

            migrationBuilder.DropColumn(
                name: "ExclusionReason",
                table: "RouteStops");

            migrationBuilder.DropColumn(
                name: "IsExcluded",
                table: "RouteStops");
        }
    }
}
