using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MaemoCompliance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExpandComplianceTestsDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CorrectiveActionCompletedAt",
                table: "Ncrs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CorrectiveActionDueDate",
                table: "Ncrs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CorrectiveActionOwner",
                table: "Ncrs",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CorrectiveActionPlan",
                table: "Ncrs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EffectivenessConfirmed",
                table: "Ncrs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectivenessVerifiedAt",
                table: "Ncrs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LinkedAuditFindingId",
                table: "Ncrs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RootCauseMethod",
                table: "Ncrs",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedBy",
                table: "Documents",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedForReviewAt",
                table: "Documents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SupersededByDocumentId",
                table: "Documents",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AuditFindings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AuditRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    LinkedNcrId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditFindings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditProgrammes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditProgrammes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditScheduleItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AuditProgrammeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcessArea = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AuditorName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PlannedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    LinkedAuditId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditScheduleItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditScheduleItems_AuditProgrammes_AuditProgrammeId",
                        column: x => x.AuditProgrammeId,
                        principalTable: "AuditProgrammes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditFindings_TenantId_AuditRunId",
                table: "AuditFindings",
                columns: new[] { "TenantId", "AuditRunId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditProgrammes_TenantId_Year",
                table: "AuditProgrammes",
                columns: new[] { "TenantId", "Year" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditScheduleItems_AuditProgrammeId",
                table: "AuditScheduleItems",
                column: "AuditProgrammeId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditScheduleItems_TenantId_AuditProgrammeId",
                table: "AuditScheduleItems",
                columns: new[] { "TenantId", "AuditProgrammeId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditFindings");

            migrationBuilder.DropTable(
                name: "AuditScheduleItems");

            migrationBuilder.DropTable(
                name: "AuditProgrammes");

            migrationBuilder.DropColumn(
                name: "CorrectiveActionCompletedAt",
                table: "Ncrs");

            migrationBuilder.DropColumn(
                name: "CorrectiveActionDueDate",
                table: "Ncrs");

            migrationBuilder.DropColumn(
                name: "CorrectiveActionOwner",
                table: "Ncrs");

            migrationBuilder.DropColumn(
                name: "CorrectiveActionPlan",
                table: "Ncrs");

            migrationBuilder.DropColumn(
                name: "EffectivenessConfirmed",
                table: "Ncrs");

            migrationBuilder.DropColumn(
                name: "EffectivenessVerifiedAt",
                table: "Ncrs");

            migrationBuilder.DropColumn(
                name: "LinkedAuditFindingId",
                table: "Ncrs");

            migrationBuilder.DropColumn(
                name: "RootCauseMethod",
                table: "Ncrs");

            migrationBuilder.DropColumn(
                name: "ApprovedBy",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "SubmittedForReviewAt",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "SupersededByDocumentId",
                table: "Documents");
        }
    }
}
