using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MaemoCompliance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnhanceNcrModelPhase2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "Ncrs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CorrectiveAction",
                table: "Ncrs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EscalationLevel",
                table: "Ncrs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RootCause",
                table: "Ncrs",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "NcrStatusHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NcrId = table.Column<Guid>(type: "uuid", nullable: false),
                    OldStatus = table.Column<int>(type: "integer", nullable: false),
                    NewStatus = table.Column<int>(type: "integer", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ChangedByUserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NcrStatusHistory", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NcrStatusHistory_NcrId_TenantId",
                table: "NcrStatusHistory",
                columns: new[] { "NcrId", "TenantId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NcrStatusHistory");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Ncrs");

            migrationBuilder.DropColumn(
                name: "CorrectiveAction",
                table: "Ncrs");

            migrationBuilder.DropColumn(
                name: "EscalationLevel",
                table: "Ncrs");

            migrationBuilder.DropColumn(
                name: "RootCause",
                table: "Ncrs");
        }
    }
}
