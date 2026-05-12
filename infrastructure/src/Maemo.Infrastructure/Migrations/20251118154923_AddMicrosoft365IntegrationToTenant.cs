using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maemo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMicrosoft365IntegrationToTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AzureAdClientId",
                table: "Tenants",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AzureAdClientSecret",
                table: "Tenants",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AzureAdTenantId",
                table: "Tenants",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SharePointDriveId",
                table: "Tenants",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SharePointSiteId",
                table: "Tenants",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AzureAdClientId",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "AzureAdClientSecret",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "AzureAdTenantId",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "SharePointDriveId",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "SharePointSiteId",
                table: "Tenants");
        }
    }
}
