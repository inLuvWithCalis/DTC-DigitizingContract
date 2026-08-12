using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractManagement.Infrastructure.Migrations.Application
{
    /// <inheritdoc />
    public partial class Phase6_Slice09TemplateDocxValidationAndAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tbl_ContractTemplateAudit",
                columns: table => new
                {
                    ContractTemplateAuditId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    TemplateId = table.Column<int>(type: "int", nullable: false),
                    TemplateVersionId = table.Column<int>(type: "int", nullable: false),
                    ActorEmployeeId = table.Column<int>(type: "int", nullable: false),
                    ActionType = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    Result = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    FailureCode = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: true),
                    PreviousValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IpAddress = table.Column<string>(type: "varchar(45)", unicode: false, maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    CorrelationId = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_ContractTemplateAudit", x => x.ContractTemplateAuditId);
                    table.CheckConstraint("CK_tbl_ContractTemplateAudit_ActionType", "LEN(LTRIM(RTRIM([ActionType]))) > 0");
                    table.CheckConstraint("CK_tbl_ContractTemplateAudit_ActorEmployeeId", "[ActorEmployeeId] > 0");
                    table.CheckConstraint("CK_tbl_ContractTemplateAudit_CorrelationId", "LEN(LTRIM(RTRIM([CorrelationId]))) > 0");
                    table.CheckConstraint("CK_tbl_ContractTemplateAudit_FailureCode", "[FailureCode] IS NULL OR LEN(LTRIM(RTRIM([FailureCode]))) > 0");
                    table.CheckConstraint("CK_tbl_ContractTemplateAudit_NewValuesJson", "[NewValuesJson] IS NULL OR (ISJSON([NewValuesJson]) = 1 AND LEFT(LTRIM([NewValuesJson]), 1) = '{')");
                    table.CheckConstraint("CK_tbl_ContractTemplateAudit_PreviousValuesJson", "[PreviousValuesJson] IS NULL OR (ISJSON([PreviousValuesJson]) = 1 AND LEFT(LTRIM([PreviousValuesJson]), 1) = '{')");
                    table.CheckConstraint("CK_tbl_ContractTemplateAudit_Result", "LEN(LTRIM(RTRIM([Result]))) > 0");
                    table.CheckConstraint("CK_tbl_ContractTemplateAudit_TemplateId", "[TemplateId] > 0");
                    table.CheckConstraint("CK_tbl_ContractTemplateAudit_TemplateVersionId", "[TemplateVersionId] > 0");
                    table.CheckConstraint("CK_tbl_ContractTemplateAudit_TenantId", "[TenantId] > 0");
                });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractTemplateAudit_TenantId_Version_OccurredAt",
                table: "tbl_ContractTemplateAudit",
                columns: new[] { "TenantId", "TemplateVersionId", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbl_ContractTemplateAudit");
        }
    }
}
