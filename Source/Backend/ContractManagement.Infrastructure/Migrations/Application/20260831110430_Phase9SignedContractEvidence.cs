using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractManagement.Infrastructure.Migrations.Application
{
    /// <inheritdoc />
    public partial class Phase9SignedContractEvidence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractAudit_Subject",
                table: "tbl_ContractAudit");

            migrationBuilder.CreateTable(
                name: "tbl_ContractSignedEvidence",
                columns: table => new
                {
                    SignedEvidenceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractId = table.Column<int>(type: "int", nullable: false),
                    VersionId = table.Column<int>(type: "int", nullable: false),
                    FileId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    ProviderSignerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ProviderSignerTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ProviderSigningDate = table.Column<DateTime>(type: "date", nullable: false),
                    CustomerSignerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CustomerSignerTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CustomerSigningDate = table.Column<DateTime>(type: "date", nullable: false),
                    SupersedesEvidenceId = table.Column<int>(type: "int", nullable: true),
                    SupersedeReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    UploadedByEmployeeId = table.Column<int>(type: "int", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SupersededByEmployeeId = table.Column<int>(type: "int", nullable: true),
                    SupersededAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_ContractSignedEvidence", x => x.SignedEvidenceId);
                    table.CheckConstraint("CK_tbl_ContractSignedEvidence_ContractId", "[ContractId] > 0");
                    table.CheckConstraint("CK_tbl_ContractSignedEvidence_FileId", "[FileId] > 0");
                    table.CheckConstraint("CK_tbl_ContractSignedEvidence_Status", "[Status] IN (1, 2)");
                    table.CheckConstraint("CK_tbl_ContractSignedEvidence_VersionId", "[VersionId] > 0");
                    table.ForeignKey(
                        name: "FK_tbl_ContractSignedEvidence_tbl_ContractSignedEvidence_SupersedesEvidenceId",
                        column: x => x.SupersedesEvidenceId,
                        principalTable: "tbl_ContractSignedEvidence",
                        principalColumn: "SignedEvidenceId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbl_ContractSignedEvidence_tbl_ContractVersion_VersionId",
                        column: x => x.VersionId,
                        principalTable: "tbl_ContractVersion",
                        principalColumn: "VersionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbl_ContractSignedEvidence_tbl_Contract_ContractId",
                        column: x => x.ContractId,
                        principalTable: "tbl_Contract",
                        principalColumn: "ContractId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbl_ContractSignedEvidence_tbl_Employee_SupersededByEmployeeId",
                        column: x => x.SupersededByEmployeeId,
                        principalTable: "tbl_Employee",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbl_ContractSignedEvidence_tbl_Employee_UploadedByEmployeeId",
                        column: x => x.UploadedByEmployeeId,
                        principalTable: "tbl_Employee",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbl_ContractSignedEvidence_tbl_FileStorage_FileId",
                        column: x => x.FileId,
                        principalTable: "tbl_FileStorage",
                        principalColumn: "FileId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractAudit_Subject",
                table: "tbl_ContractAudit",
                sql: "([SubjectType] IS NULL AND [SubjectId] IS NULL) OR ([SubjectType] IN ('Contract', 'ContractVersion', 'NegotiationComment', 'CustomerAccessLink', 'CustomerOtpChallenge', 'CustomerAccessSession', 'ApprovalRequest', 'SignedEvidence') AND [SubjectId] > 0)");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractSignedEvidence_SupersededByEmployeeId",
                table: "tbl_ContractSignedEvidence",
                column: "SupersededByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractSignedEvidence_SupersedesEvidenceId",
                table: "tbl_ContractSignedEvidence",
                column: "SupersedesEvidenceId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractSignedEvidence_UploadedByEmployeeId",
                table: "tbl_ContractSignedEvidence",
                column: "UploadedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractSignedEvidence_VersionId",
                table: "tbl_ContractSignedEvidence",
                column: "VersionId");

            migrationBuilder.CreateIndex(
                name: "UX_tbl_ContractSignedEvidence_ActiveVersion",
                table: "tbl_ContractSignedEvidence",
                columns: new[] { "ContractId", "VersionId" },
                unique: true,
                filter: "[Status] = 1");

            migrationBuilder.CreateIndex(
                name: "UX_tbl_ContractSignedEvidence_FileId",
                table: "tbl_ContractSignedEvidence",
                column: "FileId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbl_ContractSignedEvidence");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractAudit_Subject",
                table: "tbl_ContractAudit");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractAudit_Subject",
                table: "tbl_ContractAudit",
                sql: "([SubjectType] IS NULL AND [SubjectId] IS NULL) OR ([SubjectType] IN ('Contract', 'ContractVersion', 'NegotiationComment', 'CustomerAccessLink', 'CustomerOtpChallenge', 'CustomerAccessSession', 'ApprovalRequest') AND [SubjectId] > 0)");
        }
    }
}
