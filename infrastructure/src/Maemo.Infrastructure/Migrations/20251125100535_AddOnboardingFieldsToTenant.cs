using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maemo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOnboardingFieldsToTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "OnboardingCompleted",
                table: "Tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "OnboardingCompletedAt",
                table: "Tenants",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OnboardingCompleted",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "OnboardingCompletedAt",
                table: "Tenants");
        }
    }
}
