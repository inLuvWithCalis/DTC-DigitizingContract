using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractManagement.Infrastructure.Migrations.Application
{
    /// <inheritdoc />
    public partial class Phase6_Slice07AuditCoverageAndVisibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FailureCode",
                table: "tbl_ContractAudit",
                type: "varchar(64)",
                unicode: false,
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NewValuesJson",
                table: "tbl_ContractAudit",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreviousValuesJson",
                table: "tbl_ContractAudit",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubjectId",
                table: "tbl_ContractAudit",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubjectType",
                table: "tbl_ContractAudit",
                type: "varchar(64)",
                unicode: false,
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractAudit_TenantId_OccurredAt_ContractAuditId",
                table: "tbl_ContractAudit",
                columns: new[] { "TenantId", "OccurredAt", "ContractAuditId" },
                descending: new[] { false, true, true });

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractAudit_FailureCode",
                table: "tbl_ContractAudit",
                sql: "[FailureCode] IS NULL OR LEN(LTRIM(RTRIM([FailureCode]))) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractAudit_NewValuesJson",
                table: "tbl_ContractAudit",
                sql: "[NewValuesJson] IS NULL OR (ISJSON([NewValuesJson]) = 1 AND LEFT(LTRIM([NewValuesJson]), 1) = '{')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractAudit_PreviousValuesJson",
                table: "tbl_ContractAudit",
                sql: "[PreviousValuesJson] IS NULL OR (ISJSON([PreviousValuesJson]) = 1 AND LEFT(LTRIM([PreviousValuesJson]), 1) = '{')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractAudit_Subject",
                table: "tbl_ContractAudit",
                sql: "([SubjectType] IS NULL AND [SubjectId] IS NULL) OR ([SubjectType] IN ('Contract', 'ContractVersion', 'NegotiationComment', 'CustomerAccessLink', 'CustomerOtpChallenge', 'CustomerAccessSession') AND [SubjectId] > 0)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tbl_ContractAudit_TenantId_OccurredAt_ContractAuditId",
                table: "tbl_ContractAudit");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractAudit_FailureCode",
                table: "tbl_ContractAudit");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractAudit_NewValuesJson",
                table: "tbl_ContractAudit");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractAudit_PreviousValuesJson",
                table: "tbl_ContractAudit");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractAudit_Subject",
                table: "tbl_ContractAudit");

            migrationBuilder.DropColumn(
                name: "FailureCode",
                table: "tbl_ContractAudit");

            migrationBuilder.DropColumn(
                name: "NewValuesJson",
                table: "tbl_ContractAudit");

            migrationBuilder.DropColumn(
                name: "PreviousValuesJson",
                table: "tbl_ContractAudit");

            migrationBuilder.DropColumn(
                name: "SubjectId",
                table: "tbl_ContractAudit");

            migrationBuilder.DropColumn(
                name: "SubjectType",
                table: "tbl_ContractAudit");
        }
    }
}
