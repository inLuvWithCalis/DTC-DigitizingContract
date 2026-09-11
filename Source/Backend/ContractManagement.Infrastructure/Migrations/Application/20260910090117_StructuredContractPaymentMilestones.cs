using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractManagement.Infrastructure.Migrations.Application
{
    /// <inheritdoc />
    public partial class StructuredContractPaymentMilestones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "TermKind",
                table: "tbl_ContractTerm",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0)
                .Annotation("Relational:DefaultConstraintName", "DF_tbl_ContractTerm_TermKind");

            migrationBuilder.AddColumn<byte>(
                name: "TermKind",
                table: "tbl_ContractTemplateTerm",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0)
                .Annotation("Relational:DefaultConstraintName", "DF_tbl_ContractTemplateTerm_TermKind");

            migrationBuilder.AddColumn<int>(
                name: "PaymentMilestoneId",
                table: "tbl_ContractPaymentLedger",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "tbl_ContractPaymentMilestone",
                columns: table => new
                {
                    PaymentMilestoneId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractId = table.Column<int>(type: "int", nullable: false),
                    VersionId = table.Column<int>(type: "int", nullable: false),
                    TermId = table.Column<int>(type: "int", nullable: false),
                    SourceTemplatePaymentMilestoneId = table.Column<int>(type: "int", nullable: true),
                    MilestoneCode = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    TitleVi = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TitleEn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PaymentPercent = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    DueAnchor = table.Column<byte>(type: "tinyint", nullable: false),
                    DueOffsetDays = table.Column<int>(type: "int", nullable: false),
                    DayCountMode = table.Column<byte>(type: "tinyint", nullable: false),
                    ConditionVi = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ConditionEn = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AnchorDate = table.Column<DateTime>(type: "date", nullable: true),
                    DueDate = table.Column<DateTime>(type: "date", nullable: true),
                    CreatedEmployeeId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    UpdatedEmployeeId = table.Column<int>(type: "int", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_ContractPaymentMilestone", x => x.PaymentMilestoneId);
                    table.CheckConstraint("CK_tbl_ContractPaymentMilestone_Amount", "[Amount] >= 0");
                    table.CheckConstraint("CK_tbl_ContractPaymentMilestone_DayCount", "[DayCountMode] IN (1, 2)");
                    table.CheckConstraint("CK_tbl_ContractPaymentMilestone_DueAnchor", "[DueAnchor] IN (1, 2, 3, 4, 5)");
                    table.CheckConstraint("CK_tbl_ContractPaymentMilestone_DueOffset", "[DueOffsetDays] >= 0");
                    table.CheckConstraint("CK_tbl_ContractPaymentMilestone_Percent", "[PaymentPercent] > 0 AND [PaymentPercent] <= 100");
                    table.ForeignKey(
                        name: "FK_tbl_ContractPaymentMilestone_tbl_ContractTerm_TermId",
                        column: x => x.TermId,
                        principalTable: "tbl_ContractTerm",
                        principalColumn: "TermId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tbl_ContractPaymentMilestone_tbl_ContractVersion_VersionId",
                        column: x => x.VersionId,
                        principalTable: "tbl_ContractVersion",
                        principalColumn: "VersionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbl_ContractPaymentMilestone_tbl_Contract_ContractId",
                        column: x => x.ContractId,
                        principalTable: "tbl_Contract",
                        principalColumn: "ContractId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tbl_ContractTemplatePaymentMilestone",
                columns: table => new
                {
                    TemplatePaymentMilestoneId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateVersionId = table.Column<int>(type: "int", nullable: false),
                    TemplateTermId = table.Column<int>(type: "int", nullable: false),
                    MilestoneCode = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    TitleVi = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TitleEn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PaymentPercent = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    DueAnchor = table.Column<byte>(type: "tinyint", nullable: false),
                    DueOffsetDays = table.Column<int>(type: "int", nullable: false),
                    DayCountMode = table.Column<byte>(type: "tinyint", nullable: false),
                    ConditionVi = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ConditionEn = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedEmployeeId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    UpdatedEmployeeId = table.Column<int>(type: "int", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_ContractTemplatePaymentMilestone", x => x.TemplatePaymentMilestoneId);
                    table.CheckConstraint("CK_tbl_ContractTemplatePaymentMilestone_DayCount", "[DayCountMode] IN (1, 2)");
                    table.CheckConstraint("CK_tbl_ContractTemplatePaymentMilestone_DueAnchor", "[DueAnchor] IN (1, 2, 3, 4, 5)");
                    table.CheckConstraint("CK_tbl_ContractTemplatePaymentMilestone_DueOffset", "[DueOffsetDays] >= 0");
                    table.CheckConstraint("CK_tbl_ContractTemplatePaymentMilestone_Percent", "[PaymentPercent] > 0 AND [PaymentPercent] <= 100");
                    table.ForeignKey(
                        name: "FK_tbl_ContractTemplatePaymentMilestone_tbl_ContractTemplateTerm_TemplateTermId",
                        column: x => x.TemplateTermId,
                        principalTable: "tbl_ContractTemplateTerm",
                        principalColumn: "TemplateTermId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tbl_ContractTemplatePaymentMilestone_tbl_ContractTemplateVersion_TemplateVersionId",
                        column: x => x.TemplateVersionId,
                        principalTable: "tbl_ContractTemplateVersion",
                        principalColumn: "TemplateVersionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractTerm_TermKind",
                table: "tbl_ContractTerm",
                sql: "[TermKind] IN (0, 1)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractTemplateTerm_TermKind",
                table: "tbl_ContractTemplateTerm",
                sql: "[TermKind] IN (0, 1)");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractPaymentLedger_PaymentMilestoneId",
                table: "tbl_ContractPaymentLedger",
                column: "PaymentMilestoneId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractPaymentMilestone_ContractId_VersionId_DisplayOrder",
                table: "tbl_ContractPaymentMilestone",
                columns: new[] { "ContractId", "VersionId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractPaymentMilestone_TermId",
                table: "tbl_ContractPaymentMilestone",
                column: "TermId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractPaymentMilestone_VersionId_MilestoneCode",
                table: "tbl_ContractPaymentMilestone",
                columns: new[] { "VersionId", "MilestoneCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractTemplatePaymentMilestone_TemplateTermId_DisplayOrder",
                table: "tbl_ContractTemplatePaymentMilestone",
                columns: new[] { "TemplateTermId", "DisplayOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractTemplatePaymentMilestone_TemplateVersionId_MilestoneCode",
                table: "tbl_ContractTemplatePaymentMilestone",
                columns: new[] { "TemplateVersionId", "MilestoneCode" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_tbl_ContractPaymentLedger_tbl_ContractPaymentMilestone_PaymentMilestoneId",
                table: "tbl_ContractPaymentLedger",
                column: "PaymentMilestoneId",
                principalTable: "tbl_ContractPaymentMilestone",
                principalColumn: "PaymentMilestoneId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tbl_ContractPaymentLedger_tbl_ContractPaymentMilestone_PaymentMilestoneId",
                table: "tbl_ContractPaymentLedger");

            migrationBuilder.DropTable(
                name: "tbl_ContractPaymentMilestone");

            migrationBuilder.DropTable(
                name: "tbl_ContractTemplatePaymentMilestone");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractTerm_TermKind",
                table: "tbl_ContractTerm");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractTemplateTerm_TermKind",
                table: "tbl_ContractTemplateTerm");

            migrationBuilder.DropIndex(
                name: "IX_tbl_ContractPaymentLedger_PaymentMilestoneId",
                table: "tbl_ContractPaymentLedger");

            migrationBuilder.DropColumn(
                name: "TermKind",
                table: "tbl_ContractTerm")
                .Annotation("Relational:DefaultConstraintName", "DF_tbl_ContractTerm_TermKind");

            migrationBuilder.DropColumn(
                name: "TermKind",
                table: "tbl_ContractTemplateTerm")
                .Annotation("Relational:DefaultConstraintName", "DF_tbl_ContractTemplateTerm_TermKind");

            migrationBuilder.DropColumn(
                name: "PaymentMilestoneId",
                table: "tbl_ContractPaymentLedger");
        }
    }
}
