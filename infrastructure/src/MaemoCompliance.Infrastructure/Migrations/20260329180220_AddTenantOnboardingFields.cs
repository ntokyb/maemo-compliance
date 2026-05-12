using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MaemoCompliance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantOnboardingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "MaxStorageBytes",
                table: "Tenants",
                type: "bigint",
                nullable: false,
                defaultValue: 5368709120L);

            migrationBuilder.AddColumn<int>(
                name: "MaxUsers",
                table: "Tenants",
                type: "integer",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.AddColumn<string>(
                name: "SharePointClientId",
                table: "Tenants",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SharePointClientSecretEncrypted",
                table: "Tenants",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SharePointLibraryName",
                table: "Tenants",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                defaultValue: "Shared Documents");

            migrationBuilder.AddColumn<string>(
                name: "SharePointSiteUrl",
                table: "Tenants",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileHash",
                table: "DocumentVersions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DestroyReason",
                table: "Documents",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DestroyedAt",
                table: "Documents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DestroyedByUserId",
                table: "Documents",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileHash",
                table: "Documents",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FilePlanItem",
                table: "Documents",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FilePlanSeries",
                table: "Documents",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FilePlanSubSeries",
                table: "Documents",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDestroyed",
                table: "Documents",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPendingArchive",
                table: "Documents",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PersonalInformationType",
                table: "Documents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PiiDescription",
                table: "Documents",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PiiRetentionPeriodInMonths",
                table: "Documents",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PiiType",
                table: "Documents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "EvidenceFileHash",
                table: "AuditAnswers",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_TenantId_FilePlanSeries_FilePlanSubSeries",
                table: "Documents",
                columns: new[] { "TenantId", "FilePlanSeries", "FilePlanSubSeries" });

            migrationBuilder.CreateIndex(
                name: "IX_Documents_TenantId_IsDestroyed",
                table: "Documents",
                columns: new[] { "TenantId", "IsDestroyed" });

            migrationBuilder.CreateIndex(
                name: "IX_Documents_TenantId_PersonalInformationType",
                table: "Documents",
                columns: new[] { "TenantId", "PersonalInformationType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Documents_TenantId_FilePlanSeries_FilePlanSubSeries",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_TenantId_IsDestroyed",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_TenantId_PersonalInformationType",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "MaxStorageBytes",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "MaxUsers",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "SharePointClientId",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "SharePointClientSecretEncrypted",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "SharePointLibraryName",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "SharePointSiteUrl",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "FileHash",
                table: "DocumentVersions");

            migrationBuilder.DropColumn(
                name: "DestroyReason",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "DestroyedAt",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "DestroyedByUserId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "FileHash",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "FilePlanItem",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "FilePlanSeries",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "FilePlanSubSeries",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "IsDestroyed",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "IsPendingArchive",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "PersonalInformationType",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "PiiDescription",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "PiiRetentionPeriodInMonths",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "PiiType",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "EvidenceFileHash",
                table: "AuditAnswers");
        }
    }
}
