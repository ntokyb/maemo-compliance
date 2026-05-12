using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MaemoCompliance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentWorkflowState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add WorkflowState column with default value of 0 (Draft)
            migrationBuilder.AddColumn<int>(
                name: "WorkflowState",
                table: "Documents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Add RejectedReason column
            migrationBuilder.AddColumn<string>(
                name: "RejectedReason",
                table: "Documents",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            // Create index for WorkflowState queries
            migrationBuilder.CreateIndex(
                name: "IX_Documents_TenantId_WorkflowState",
                table: "Documents",
                columns: new[] { "TenantId", "WorkflowState" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop index
            migrationBuilder.DropIndex(
                name: "IX_Documents_TenantId_WorkflowState",
                table: "Documents");

            // Drop columns
            migrationBuilder.DropColumn(
                name: "WorkflowState",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "RejectedReason",
                table: "Documents");
        }
    }
}

