using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractManagement.Infrastructure.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddContractApprovalRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tbl_ContractApprovalRequest",
                columns: table => new
                {
                    ApprovalRequestId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractId = table.Column<int>(type: "int", nullable: false),
                    VersionId = table.Column<int>(type: "int", nullable: false),
                    WorkflowId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)0),
                    SubmittedByEmployeeId = table.Column<int>(type: "int", nullable: false),
                    SubmittedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    ResolvedByEmployeeId = table.Column<int>(type: "int", nullable: true),
                    ResolvedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DecisionComment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_ContractApprovalRequest", x => x.ApprovalRequestId);
                    table.CheckConstraint("CK_tbl_ContractApprovalRequest_ContractId", "[ContractId] > 0");
                    table.CheckConstraint("CK_tbl_ContractApprovalRequest_Status", "[Status] IN (0, 1, 2, 3, 4)");
                    table.CheckConstraint("CK_tbl_ContractApprovalRequest_VersionId", "[VersionId] > 0");
                });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractApprovalRequest_VersionId",
                table: "tbl_ContractApprovalRequest",
                column: "VersionId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractApprovalRequest_WorkflowId",
                table: "tbl_ContractApprovalRequest",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "UX_tbl_ContractApprovalRequest_PendingContract",
                table: "tbl_ContractApprovalRequest",
                column: "ContractId",
                unique: true,
                filter: "[Status] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbl_ContractApprovalRequest");
        }
    }
}
