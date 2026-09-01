using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractManagement.Infrastructure.Migrations.Application
{
    /// <inheritdoc />
    public partial class Phase10AcceptancePaymentCompletion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractAudit_Subject",
                table: "tbl_ContractAudit");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractAudit_Subject",
                table: "tbl_ContractAudit",
                sql: "([SubjectType] IS NULL AND [SubjectId] IS NULL) OR ([SubjectType] IN ('Contract', 'ContractVersion', 'NegotiationComment', 'CustomerAccessLink', 'CustomerOtpChallenge', 'CustomerAccessSession', 'ApprovalRequest', 'SignedEvidence', 'AcceptanceEvidence', 'Payment') AND [SubjectId] > 0)");

            migrationBuilder.CreateTable(
                name: "tbl_ContractAcceptanceEvidence",
                columns: table => new
                {
                    AcceptanceEvidenceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractId = table.Column<int>(type: "int", nullable: false),
                    VersionId = table.Column<int>(type: "int", nullable: false),
                    FileId = table.Column<int>(type: "int", nullable: false),
                    UploadedByEmployeeId = table.Column<int>(type: "int", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_ContractAcceptanceEvidence", x => x.AcceptanceEvidenceId);
                    table.CheckConstraint("CK_tbl_ContractAcceptanceEvidence_ContractId", "[ContractId] > 0");
                    table.CheckConstraint("CK_tbl_ContractAcceptanceEvidence_FileId", "[FileId] > 0");
                    table.CheckConstraint("CK_tbl_ContractAcceptanceEvidence_VersionId", "[VersionId] > 0");
                    table.ForeignKey(
                        name: "FK_tbl_ContractAcceptanceEvidence_tbl_ContractVersion_VersionId",
                        column: x => x.VersionId,
                        principalTable: "tbl_ContractVersion",
                        principalColumn: "VersionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbl_ContractAcceptanceEvidence_tbl_Contract_ContractId",
                        column: x => x.ContractId,
                        principalTable: "tbl_Contract",
                        principalColumn: "ContractId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbl_ContractAcceptanceEvidence_tbl_Employee_UploadedByEmployeeId",
                        column: x => x.UploadedByEmployeeId,
                        principalTable: "tbl_Employee",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbl_ContractAcceptanceEvidence_tbl_FileStorage_FileId",
                        column: x => x.FileId,
                        principalTable: "tbl_FileStorage",
                        principalColumn: "FileId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tbl_ContractPaymentLedger",
                columns: table => new
                {
                    ContractPaymentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractId = table.Column<int>(type: "int", nullable: false),
                    VersionId = table.Column<int>(type: "int", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CurrencyCode = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ReferenceCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EvidenceFileId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    CreatedByEmployeeId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VoidReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    VoidedByEmployeeId = table.Column<int>(type: "int", nullable: true),
                    VoidedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_ContractPaymentLedger", x => x.ContractPaymentId);
                    table.CheckConstraint("CK_tbl_ContractPaymentLedger_Amount", "[Amount] > 0");
                    table.CheckConstraint("CK_tbl_ContractPaymentLedger_Status", "[Status] IN (1, 2)");
                    table.CheckConstraint("CK_tbl_ContractPaymentLedger_VoidMetadata", "([Status] = 1 AND [VoidReason] IS NULL AND [VoidedByEmployeeId] IS NULL AND [VoidedAt] IS NULL) OR ([Status] = 2 AND [VoidReason] IS NOT NULL AND [VoidedByEmployeeId] IS NOT NULL AND [VoidedAt] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_tbl_ContractPaymentLedger_tbl_ContractVersion_VersionId",
                        column: x => x.VersionId,
                        principalTable: "tbl_ContractVersion",
                        principalColumn: "VersionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbl_ContractPaymentLedger_tbl_Contract_ContractId",
                        column: x => x.ContractId,
                        principalTable: "tbl_Contract",
                        principalColumn: "ContractId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbl_ContractPaymentLedger_tbl_Employee_CreatedByEmployeeId",
                        column: x => x.CreatedByEmployeeId,
                        principalTable: "tbl_Employee",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbl_ContractPaymentLedger_tbl_Employee_VoidedByEmployeeId",
                        column: x => x.VoidedByEmployeeId,
                        principalTable: "tbl_Employee",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbl_ContractPaymentLedger_tbl_FileStorage_EvidenceFileId",
                        column: x => x.EvidenceFileId,
                        principalTable: "tbl_FileStorage",
                        principalColumn: "FileId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractAcceptanceEvidence_ContractId_VersionId",
                table: "tbl_ContractAcceptanceEvidence",
                columns: new[] { "ContractId", "VersionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractAcceptanceEvidence_FileId",
                table: "tbl_ContractAcceptanceEvidence",
                column: "FileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractAcceptanceEvidence_UploadedByEmployeeId",
                table: "tbl_ContractAcceptanceEvidence",
                column: "UploadedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractAcceptanceEvidence_VersionId",
                table: "tbl_ContractAcceptanceEvidence",
                column: "VersionId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractPaymentLedger_ContractId",
                table: "tbl_ContractPaymentLedger",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractPaymentLedger_CreatedByEmployeeId",
                table: "tbl_ContractPaymentLedger",
                column: "CreatedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractPaymentLedger_EvidenceFileId",
                table: "tbl_ContractPaymentLedger",
                column: "EvidenceFileId",
                unique: true,
                filter: "[EvidenceFileId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractPaymentLedger_VersionId_ReferenceCode",
                table: "tbl_ContractPaymentLedger",
                columns: new[] { "VersionId", "ReferenceCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractPaymentLedger_VoidedByEmployeeId",
                table: "tbl_ContractPaymentLedger",
                column: "VoidedByEmployeeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractAudit_Subject",
                table: "tbl_ContractAudit");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractAudit_Subject",
                table: "tbl_ContractAudit",
                sql: "([SubjectType] IS NULL AND [SubjectId] IS NULL) OR ([SubjectType] IN ('Contract', 'ContractVersion', 'NegotiationComment', 'CustomerAccessLink', 'CustomerOtpChallenge', 'CustomerAccessSession', 'ApprovalRequest', 'SignedEvidence') AND [SubjectId] > 0)");

            migrationBuilder.DropTable(
                name: "tbl_ContractAcceptanceEvidence");

            migrationBuilder.DropTable(
                name: "tbl_ContractPaymentLedger");
        }
    }
}
