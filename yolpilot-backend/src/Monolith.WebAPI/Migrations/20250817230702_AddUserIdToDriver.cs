using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monolith.WebAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdToDriver : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Drivers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Drivers_UserId",
                table: "Drivers",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Drivers_AspNetUsers_UserId",
                table: "Drivers",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Drivers_AspNetUsers_UserId",
                table: "Drivers");

            migrationBuilder.DropIndex(
                name: "IX_Drivers_UserId",
                table: "Drivers");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Drivers");
        }
    }
}
