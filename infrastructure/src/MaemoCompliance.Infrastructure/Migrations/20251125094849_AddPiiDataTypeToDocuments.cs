using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MaemoCompliance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPiiDataTypeToDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PiiDataType",
                table: "Documents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RejectedReason",
                table: "Documents",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkflowState",
                table: "Documents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_TenantId_PiiDataType",
                table: "Documents",
                columns: new[] { "TenantId", "PiiDataType" });

            migrationBuilder.CreateIndex(
                name: "IX_Documents_TenantId_WorkflowState",
                table: "Documents",
                columns: new[] { "TenantId", "WorkflowState" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Documents_TenantId_PiiDataType",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_TenantId_WorkflowState",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "PiiDataType",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "RejectedReason",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "WorkflowState",
                table: "Documents");
        }
    }
}
