using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monolith.WebAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddEstimatedTimesToRouteStop : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeSpan>(
                name: "EstimatedArrivalTime",
                table: "RouteStops",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "EstimatedDepartureTime",
                table: "RouteStops",
                type: "time",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstimatedArrivalTime",
                table: "RouteStops");

            migrationBuilder.DropColumn(
                name: "EstimatedDepartureTime",
                table: "RouteStops");
        }
    }
}
