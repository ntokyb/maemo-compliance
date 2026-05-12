using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MaemoCompliance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBbbeeFieldsToDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "BbbeeExpiryDate",
                table: "Documents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BbbeeLevel",
                table: "Documents",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_TenantId_Category_BbbeeExpiryDate",
                table: "Documents",
                columns: new[] { "TenantId", "Category", "BbbeeExpiryDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Documents_TenantId_Category_BbbeeExpiryDate",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "BbbeeExpiryDate",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "BbbeeLevel",
                table: "Documents");
        }
    }
}
