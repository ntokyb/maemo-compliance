using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MaemoCompliance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentRetentionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRetentionLocked",
                table: "Documents",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "RetainUntil",
                table: "Documents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_TenantId_RetainUntil_IsRetentionLocked",
                table: "Documents",
                columns: new[] { "TenantId", "RetainUntil", "IsRetentionLocked" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Documents_TenantId_RetainUntil_IsRetentionLocked",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "IsRetentionLocked",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "RetainUntil",
                table: "Documents");
        }
    }
}
