using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractManagement.Infrastructure.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddContractAuditFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tbl_ContractAudit",
                columns: table => new
                {
                    ContractAuditId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    ContractId = table.Column<int>(type: "int", nullable: false),
                    VersionId = table.Column<int>(type: "int", nullable: true),
                    ActorType = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    ActorEmployeeId = table.Column<int>(type: "int", nullable: true),
                    ActionType = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    Result = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    PreviousContractStatus = table.Column<byte>(type: "tinyint", nullable: true),
                    NewContractStatus = table.Column<byte>(type: "tinyint", nullable: true),
                    PreviousResponsibleEmployeeId = table.Column<int>(type: "int", nullable: true),
                    NewResponsibleEmployeeId = table.Column<int>(type: "int", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IpAddress = table.Column<string>(type: "varchar(45)", unicode: false, maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    CorrelationId = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_ContractAudit", x => x.ContractAuditId);
                    table.CheckConstraint("CK_tbl_ContractAudit_ActionType", "LEN(LTRIM(RTRIM([ActionType]))) > 0");
                    table.CheckConstraint("CK_tbl_ContractAudit_Actor", "([ActorType] = 'Employee' AND [ActorEmployeeId] > 0) OR ([ActorType] <> 'Employee' AND [ActorEmployeeId] IS NULL)");
                    table.CheckConstraint("CK_tbl_ContractAudit_ActorType", "LEN(LTRIM(RTRIM([ActorType]))) > 0");
                    table.CheckConstraint("CK_tbl_ContractAudit_ContractId", "[ContractId] > 0");
                    table.CheckConstraint("CK_tbl_ContractAudit_CorrelationId", "LEN(LTRIM(RTRIM([CorrelationId]))) > 0");
                    table.CheckConstraint("CK_tbl_ContractAudit_NewResponsibleEmployeeId", "[NewResponsibleEmployeeId] IS NULL OR [NewResponsibleEmployeeId] > 0");
                    table.CheckConstraint("CK_tbl_ContractAudit_PreviousResponsibleEmployeeId", "[PreviousResponsibleEmployeeId] IS NULL OR [PreviousResponsibleEmployeeId] > 0");
                    table.CheckConstraint("CK_tbl_ContractAudit_Result", "LEN(LTRIM(RTRIM([Result]))) > 0");
                    table.CheckConstraint("CK_tbl_ContractAudit_TenantId", "[TenantId] > 0");
                    table.CheckConstraint("CK_tbl_ContractAudit_VersionId", "[VersionId] IS NULL OR [VersionId] > 0");
                });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractAudit_TenantId_ContractId_OccurredAt",
                table: "tbl_ContractAudit",
                columns: new[] { "TenantId", "ContractId", "OccurredAt" });

            migrationBuilder.Sql(
                """
                CREATE TRIGGER [TR_tbl_ContractAudit_AppendOnly]
                ON [tbl_ContractAudit]
                AFTER UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    THROW 50001,
                        'Contract audit is append-only and cannot be updated or deleted.',
                        1;
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbl_ContractAudit");
        }
    }
}
