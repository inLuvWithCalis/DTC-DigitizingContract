using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractManagement.Infrastructure.Migrations.Application
{
    /// <inheritdoc />
    public partial class Phase8DInternalApprovalWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractApprovalRequest_Inbox",
                table: "tbl_ContractApprovalRequest",
                columns: new[] { "Status", "SubmittedDate", "ApprovalRequestId" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ApprovalHistory_ObjectTimeline",
                table: "tbl_ApprovalHistory",
                columns: new[] { "ObjectType", "ObjectId", "ActionDate", "ApprovalHistoryId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tbl_ContractApprovalRequest_Inbox",
                table: "tbl_ContractApprovalRequest");

            migrationBuilder.DropIndex(
                name: "IX_tbl_ApprovalHistory_ObjectTimeline",
                table: "tbl_ApprovalHistory");
        }
    }
}
