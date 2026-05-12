using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maemo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRiskRegister : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Risks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Cause = table.Column<string>(type: "text", nullable: true),
                    Consequences = table.Column<string>(type: "text", nullable: true),
                    InherentLikelihood = table.Column<int>(type: "integer", nullable: false),
                    InherentImpact = table.Column<int>(type: "integer", nullable: false),
                    InherentScore = table.Column<int>(type: "integer", nullable: false),
                    ExistingControls = table.Column<string>(type: "text", nullable: true),
                    ResidualLikelihood = table.Column<int>(type: "integer", nullable: false),
                    ResidualImpact = table.Column<int>(type: "integer", nullable: false),
                    ResidualScore = table.Column<int>(type: "integer", nullable: false),
                    OwnerUserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Risks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Risks_TenantId",
                table: "Risks",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Risks_TenantId_Category",
                table: "Risks",
                columns: new[] { "TenantId", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_Risks_TenantId_ResidualScore",
                table: "Risks",
                columns: new[] { "TenantId", "ResidualScore" });

            migrationBuilder.CreateIndex(
                name: "IX_Risks_TenantId_Status",
                table: "Risks",
                columns: new[] { "TenantId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Risks");
        }
    }
}
