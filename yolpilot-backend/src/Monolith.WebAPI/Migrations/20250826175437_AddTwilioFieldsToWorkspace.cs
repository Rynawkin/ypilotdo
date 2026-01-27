using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monolith.WebAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddTwilioFieldsToWorkspace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Settings",
                table: "Workspaces",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TwilioAccountSid",
                table: "Workspaces",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TwilioAuthToken",
                table: "Workspaces",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TwilioConnectedAt",
                table: "Workspaces",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TwilioUseSandbox",
                table: "Workspaces",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "TwilioVerified",
                table: "Workspaces",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TwilioWhatsAppNumber",
                table: "Workspaces",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WhatsApp",
                table: "Customers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "WhatsAppOptIn",
                table: "Customers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "WhatsAppOptInDate",
                table: "Customers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "WhatsAppVerified",
                table: "Customers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Settings",
                table: "Workspaces");

            migrationBuilder.DropColumn(
                name: "TwilioAccountSid",
                table: "Workspaces");

            migrationBuilder.DropColumn(
                name: "TwilioAuthToken",
                table: "Workspaces");

            migrationBuilder.DropColumn(
                name: "TwilioConnectedAt",
                table: "Workspaces");

            migrationBuilder.DropColumn(
                name: "TwilioUseSandbox",
                table: "Workspaces");

            migrationBuilder.DropColumn(
                name: "TwilioVerified",
                table: "Workspaces");

            migrationBuilder.DropColumn(
                name: "TwilioWhatsAppNumber",
                table: "Workspaces");

            migrationBuilder.DropColumn(
                name: "WhatsApp",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "WhatsAppOptIn",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "WhatsAppOptInDate",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "WhatsAppVerified",
                table: "Customers");
        }
    }
}
