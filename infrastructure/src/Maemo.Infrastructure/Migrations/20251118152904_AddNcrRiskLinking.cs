using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maemo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNcrRiskLinking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NcrRiskLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NcrId = table.Column<Guid>(type: "uuid", nullable: false),
                    RiskId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NcrRiskLinks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NcrRiskLinks_TenantId_NcrId",
                table: "NcrRiskLinks",
                columns: new[] { "TenantId", "NcrId" });

            migrationBuilder.CreateIndex(
                name: "IX_NcrRiskLinks_TenantId_NcrId_RiskId",
                table: "NcrRiskLinks",
                columns: new[] { "TenantId", "NcrId", "RiskId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NcrRiskLinks_TenantId_RiskId",
                table: "NcrRiskLinks",
                columns: new[] { "TenantId", "RiskId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NcrRiskLinks");
        }
    }
}
