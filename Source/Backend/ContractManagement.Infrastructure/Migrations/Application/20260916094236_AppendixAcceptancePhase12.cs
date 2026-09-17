using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractManagement.Infrastructure.Migrations.Application
{
    /// <inheritdoc />
    public partial class AppendixAcceptancePhase12 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Breaking schema by design: discard test-only legacy rows. There
            // is intentionally no converter, backfill, or compatibility path.
            migrationBuilder.Sql(
                "DELETE FROM [tbl_ContractAcceptanceEvidence]; " +
                "DELETE FROM [tbl_ContractAppendix];");

            migrationBuilder.DropForeignKey(
                name: "FK_tbl_ContractAcceptanceEvidence_tbl_ContractVersion_VersionId",
                table: "tbl_ContractAcceptanceEvidence");

            migrationBuilder.DropForeignKey(
                name: "FK_tbl_ContractAcceptanceEvidence_tbl_Contract_ContractId",
                table: "tbl_ContractAcceptanceEvidence");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractTemplateAuditValue_FieldCode",
                table: "tbl_ContractTemplateAuditValue");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractTemplateAuditValue_Value",
                table: "tbl_ContractTemplateAuditValue");

            migrationBuilder.DropIndex(
                name: "IX_tbl_ContractAcceptanceEvidence_ContractId_VersionId",
                table: "tbl_ContractAcceptanceEvidence");

            migrationBuilder.DropIndex(
                name: "IX_tbl_ContractAcceptanceEvidence_VersionId",
                table: "tbl_ContractAcceptanceEvidence");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractAcceptanceEvidence_ContractId",
                table: "tbl_ContractAcceptanceEvidence");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractAcceptanceEvidence_VersionId",
                table: "tbl_ContractAcceptanceEvidence");

            migrationBuilder.DropColumn(
                name: "AppendixDate",
                table: "tbl_ContractAppendix");

            migrationBuilder.DropColumn(
                name: "ContractId",
                table: "tbl_ContractAcceptanceEvidence");

            migrationBuilder.RenameColumn(
                name: "VersionId",
                table: "tbl_ContractAcceptanceEvidence",
                newName: "AcceptanceRecordId");

            migrationBuilder.AlterColumn<string>(
                name: "AppendixNameEn",
                table: "tbl_ContractAppendix",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(500)",
                oldUnicode: false,
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AppendixName",
                table: "tbl_ContractAppendix",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AppendixCode",
                table: "tbl_ContractAppendix",
                type: "varchar(50)",
                unicode: false,
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldUnicode: false,
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "tbl_ContractAppendix",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "sysutcdatetime()");

            migrationBuilder.AddColumn<int>(
                name: "CreatedEmployeeId",
                table: "tbl_ContractAppendix",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "tbl_ContractAppendix",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsRequired",
                table: "tbl_ContractAppendix",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "tbl_ContractAppendix",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "SourceTemplateAppendixId",
                table: "tbl_ContractAppendix",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "tbl_ContractAppendix",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedEmployeeId",
                table: "tbl_ContractAppendix",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VersionId",
                table: "tbl_ContractAppendix",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "tbl_ContractAcceptanceRecord",
                columns: table => new
                {
                    AcceptanceRecordId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractId = table.Column<int>(type: "int", nullable: false),
                    ContractVersionId = table.Column<int>(type: "int", nullable: false),
                    TemplateVersionId = table.Column<int>(type: "int", nullable: false),
                    AcceptanceCode = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    AcceptanceDate = table.Column<DateTime>(type: "date", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AcceptanceKind = table.Column<byte>(type: "tinyint", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    SnapshotHash = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: true),
                    FinalizedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FinalizedByEmployeeId = table.Column<int>(type: "int", nullable: true),
                    SignedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SignedByEmployeeId = table.Column<int>(type: "int", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledByEmployeeId = table.Column<int>(type: "int", nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedEmployeeId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "sysutcdatetime()"),
                    UpdatedEmployeeId = table.Column<int>(type: "int", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_ContractAcceptanceRecord", x => x.AcceptanceRecordId);
                    table.CheckConstraint("CK_tbl_ContractAcceptanceRecord_Hash", "[SnapshotHash] IS NULL OR LEN([SnapshotHash]) = 64");
                    table.CheckConstraint("CK_tbl_ContractAcceptanceRecord_Kind", "[AcceptanceKind] IN (1, 2)");
                    table.CheckConstraint("CK_tbl_ContractAcceptanceRecord_Status", "[Status] IN (0, 1, 2, 3)");
                    table.ForeignKey(
                        name: "FK_tbl_ContractAcceptanceRecord_tbl_ContractTemplateVersion_TemplateVersionId",
                        column: x => x.TemplateVersionId,
                        principalTable: "tbl_ContractTemplateVersion",
                        principalColumn: "TemplateVersionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbl_ContractAcceptanceRecord_tbl_ContractVersion_ContractVersionId",
                        column: x => x.ContractVersionId,
                        principalTable: "tbl_ContractVersion",
                        principalColumn: "VersionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbl_ContractAcceptanceRecord_tbl_Contract_ContractId",
                        column: x => x.ContractId,
                        principalTable: "tbl_Contract",
                        principalColumn: "ContractId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbl_ContractAcceptanceRecord_tbl_Employee_CancelledByEmployeeId",
                        column: x => x.CancelledByEmployeeId,
                        principalTable: "tbl_Employee",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbl_ContractAcceptanceRecord_tbl_Employee_CreatedEmployeeId",
                        column: x => x.CreatedEmployeeId,
                        principalTable: "tbl_Employee",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbl_ContractAcceptanceRecord_tbl_Employee_FinalizedByEmployeeId",
                        column: x => x.FinalizedByEmployeeId,
                        principalTable: "tbl_Employee",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbl_ContractAcceptanceRecord_tbl_Employee_SignedByEmployeeId",
                        column: x => x.SignedByEmployeeId,
                        principalTable: "tbl_Employee",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbl_ContractAcceptanceRecord_tbl_Employee_UpdatedEmployeeId",
                        column: x => x.UpdatedEmployeeId,
                        principalTable: "tbl_Employee",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tbl_ContractTemplateAppendix",
                columns: table => new
                {
                    TemplateAppendixId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateVersionId = table.Column<int>(type: "int", nullable: false),
                    AppendixCode = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    AppendixName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AppendixNameEn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AppendixDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    IsSelectedByDefault = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedEmployeeId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "sysutcdatetime()"),
                    UpdatedEmployeeId = table.Column<int>(type: "int", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_ContractTemplateAppendix", x => x.TemplateAppendixId);
                    table.CheckConstraint("CK_tbl_ContractTemplateAppendix_Code", "LEN(LTRIM(RTRIM([AppendixCode]))) > 0");
                    table.CheckConstraint("CK_tbl_ContractTemplateAppendix_Name", "LEN(LTRIM(RTRIM([AppendixName]))) > 0");
                    table.CheckConstraint("CK_tbl_ContractTemplateAppendix_Order", "[DisplayOrder] >= 0");
                    table.CheckConstraint("CK_tbl_ContractTemplateAppendix_RequiredDefault", "[IsRequired] = 0 OR [IsSelectedByDefault] = 1");
                    table.ForeignKey(
                        name: "FK_tbl_ContractTemplateAppendix_tbl_ContractTemplateVersion_TemplateVersionId",
                        column: x => x.TemplateVersionId,
                        principalTable: "tbl_ContractTemplateVersion",
                        principalColumn: "TemplateVersionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tbl_ContractTemplateAppendix_tbl_Employee_CreatedEmployeeId",
                        column: x => x.CreatedEmployeeId,
                        principalTable: "tbl_Employee",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbl_ContractTemplateAppendix_tbl_Employee_UpdatedEmployeeId",
                        column: x => x.UpdatedEmployeeId,
                        principalTable: "tbl_Employee",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tbl_ContractAcceptanceMilestone",
                columns: table => new
                {
                    AcceptanceMilestoneId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AcceptanceRecordId = table.Column<int>(type: "int", nullable: false),
                    PaymentMilestoneId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_ContractAcceptanceMilestone", x => x.AcceptanceMilestoneId);
                    table.ForeignKey(
                        name: "FK_tbl_ContractAcceptanceMilestone_tbl_ContractAcceptanceRecord_AcceptanceRecordId",
                        column: x => x.AcceptanceRecordId,
                        principalTable: "tbl_ContractAcceptanceRecord",
                        principalColumn: "AcceptanceRecordId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tbl_ContractAcceptanceMilestone_tbl_ContractPaymentMilestone_PaymentMilestoneId",
                        column: x => x.PaymentMilestoneId,
                        principalTable: "tbl_ContractPaymentMilestone",
                        principalColumn: "PaymentMilestoneId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tbl_ContractAcceptanceParty",
                columns: table => new
                {
                    AcceptancePartyId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AcceptanceRecordId = table.Column<int>(type: "int", nullable: false),
                    PartyRole = table.Column<byte>(type: "tinyint", nullable: false),
                    LegalName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Fax = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TaxCode = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    RepresentativeName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    RepresentativeTitle = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_ContractAcceptanceParty", x => x.AcceptancePartyId);
                    table.CheckConstraint("CK_tbl_ContractAcceptanceParty_Role", "[PartyRole] IN (1, 2)");
                    table.ForeignKey(
                        name: "FK_tbl_ContractAcceptanceParty_tbl_ContractAcceptanceRecord_AcceptanceRecordId",
                        column: x => x.AcceptanceRecordId,
                        principalTable: "tbl_ContractAcceptanceRecord",
                        principalColumn: "AcceptanceRecordId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbl_ContractAcceptanceReference",
                columns: table => new
                {
                    AcceptanceReferenceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AcceptanceRecordId = table.Column<int>(type: "int", nullable: false),
                    ReferenceType = table.Column<byte>(type: "tinyint", nullable: false),
                    AppendixId = table.Column<int>(type: "int", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_ContractAcceptanceReference", x => x.AcceptanceReferenceId);
                    table.CheckConstraint("CK_tbl_ContractAcceptanceReference_Target", "([ReferenceType] = 1 AND [AppendixId] IS NULL) OR ([ReferenceType] = 2 AND [AppendixId] IS NOT NULL)");
                    table.CheckConstraint("CK_tbl_ContractAcceptanceReference_Type", "[ReferenceType] IN (1, 2)");
                    table.ForeignKey(
                        name: "FK_tbl_ContractAcceptanceReference_tbl_ContractAcceptanceRecord_AcceptanceRecordId",
                        column: x => x.AcceptanceRecordId,
                        principalTable: "tbl_ContractAcceptanceRecord",
                        principalColumn: "AcceptanceRecordId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tbl_ContractAcceptanceReference_tbl_ContractAppendix_AppendixId",
                        column: x => x.AppendixId,
                        principalTable: "tbl_ContractAppendix",
                        principalColumn: "AppendixId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tbl_ContractAcceptanceSection",
                columns: table => new
                {
                    AcceptanceSectionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AcceptanceRecordId = table.Column<int>(type: "int", nullable: false),
                    SectionCode = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    TitleVi = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    TitleEn = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ContentVi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContentEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_ContractAcceptanceSection", x => x.AcceptanceSectionId);
                    table.CheckConstraint("CK_tbl_ContractAcceptanceSection_Order", "[DisplayOrder] >= 0");
                    table.ForeignKey(
                        name: "FK_tbl_ContractAcceptanceSection_tbl_ContractAcceptanceRecord_AcceptanceRecordId",
                        column: x => x.AcceptanceRecordId,
                        principalTable: "tbl_ContractAcceptanceRecord",
                        principalColumn: "AcceptanceRecordId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbl_ContractTemplateAppendixTerm",
                columns: table => new
                {
                    TemplateAppendixTermId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateAppendixId = table.Column<int>(type: "int", nullable: false),
                    TermCode = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    TermTitle = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    TermTitleEn = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TermContent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TermContentEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedEmployeeId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "sysutcdatetime()"),
                    UpdatedEmployeeId = table.Column<int>(type: "int", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_ContractTemplateAppendixTerm", x => x.TemplateAppendixTermId);
                    table.CheckConstraint("CK_tbl_ContractTemplateAppendixTerm_Code", "LEN(LTRIM(RTRIM([TermCode]))) > 0");
                    table.CheckConstraint("CK_tbl_ContractTemplateAppendixTerm_Order", "[DisplayOrder] >= 0");
                    table.CheckConstraint("CK_tbl_ContractTemplateAppendixTerm_Title", "LEN(LTRIM(RTRIM([TermTitle]))) > 0");
                    table.ForeignKey(
                        name: "FK_tbl_ContractTemplateAppendixTerm_tbl_ContractTemplateAppendix_TemplateAppendixId",
                        column: x => x.TemplateAppendixId,
                        principalTable: "tbl_ContractTemplateAppendix",
                        principalColumn: "TemplateAppendixId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tbl_ContractTemplateAppendixTerm_tbl_Employee_CreatedEmployeeId",
                        column: x => x.CreatedEmployeeId,
                        principalTable: "tbl_Employee",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbl_ContractTemplateAppendixTerm_tbl_Employee_UpdatedEmployeeId",
                        column: x => x.UpdatedEmployeeId,
                        principalTable: "tbl_Employee",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tbl_ContractAppendixTerm",
                columns: table => new
                {
                    AppendixTermId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppendixId = table.Column<int>(type: "int", nullable: false),
                    SourceTemplateAppendixTermId = table.Column<int>(type: "int", nullable: true),
                    TermCode = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    TermTitle = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    TermTitleEn = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TermContent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TermContentEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedEmployeeId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "sysutcdatetime()"),
                    UpdatedEmployeeId = table.Column<int>(type: "int", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_ContractAppendixTerm", x => x.AppendixTermId);
                    table.CheckConstraint("CK_tbl_ContractAppendixTerm_Order", "[DisplayOrder] >= 0");
                    table.ForeignKey(
                        name: "FK_tbl_ContractAppendixTerm_tbl_ContractAppendix_AppendixId",
                        column: x => x.AppendixId,
                        principalTable: "tbl_ContractAppendix",
                        principalColumn: "AppendixId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbl_ContractAppendixTerm_tbl_ContractTemplateAppendixTerm_SourceTemplateAppendixTermId",
                        column: x => x.SourceTemplateAppendixTermId,
                        principalTable: "tbl_ContractTemplateAppendixTerm",
                        principalColumn: "TemplateAppendixTermId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractTemplateAuditValue_FieldCode",
                table: "tbl_ContractTemplateAuditValue",
                sql: "[FieldCode] BETWEEN 1 AND 15");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractTemplateAuditValue_Value",
                table: "tbl_ContractTemplateAuditValue",
                sql: "([IsNull] = 1 AND [IntegerValue] IS NULL AND [LongValue] IS NULL AND [StringValue] IS NULL) OR ([IsNull] = 0 AND (([FieldCode] IN (1, 5, 6, 9, 12, 13, 14, 15) AND [IntegerValue] IS NOT NULL AND [LongValue] IS NULL AND [StringValue] IS NULL) OR ([FieldCode] IN (3, 7, 10) AND [IntegerValue] IS NULL AND [LongValue] IS NOT NULL AND [StringValue] IS NULL) OR ([FieldCode] IN (2, 4, 8, 11) AND [IntegerValue] IS NULL AND [LongValue] IS NULL AND [StringValue] IS NOT NULL)))");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractAppendix_ContractId_VersionId_DisplayOrder",
                table: "tbl_ContractAppendix",
                columns: new[] { "ContractId", "VersionId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractAppendix_CreatedEmployeeId",
                table: "tbl_ContractAppendix",
                column: "CreatedEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractAppendix_SourceTemplateAppendixId",
                table: "tbl_ContractAppendix",
                column: "SourceTemplateAppendixId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractAppendix_UpdatedEmployeeId",
                table: "tbl_ContractAppendix",
                column: "UpdatedEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractAppendix_VersionId_AppendixCode",
                table: "tbl_ContractAppendix",
                columns: new[] { "VersionId", "AppendixCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractAppendix_VersionId_DisplayOrder",
                table: "tbl_ContractAppendix",
                columns: new[] { "VersionId", "DisplayOrder" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractAppendix_Code",
                table: "tbl_ContractAppendix",
                sql: "LEN(LTRIM(RTRIM([AppendixCode]))) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractAppendix_Order",
                table: "tbl_ContractAppendix",
                sql: "[DisplayOrder] >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractAcceptanceEvidence_AcceptanceRecordId",
                table: "tbl_ContractAcceptanceEvidence",
                column: "AcceptanceRecordId",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractAcceptanceEvidence_RecordId",
                table: "tbl_ContractAcceptanceEvidence",
                sql: "[AcceptanceRecordId] > 0");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractAcceptanceMilestone_AcceptanceRecordId_PaymentMilestoneId",
                table: "tbl_ContractAcceptanceMilestone",
                columns: new[] { "AcceptanceRecordId", "PaymentMilestoneId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractAcceptanceMilestone_PaymentMilestoneId",
                table: "tbl_ContractAcceptanceMilestone",
                column: "PaymentMilestoneId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractAcceptanceParty_AcceptanceRecordId_PartyRole",
                table: "tbl_ContractAcceptanceParty",
                columns: new[] { "AcceptanceRecordId", "PartyRole" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractAcceptanceRecord_CancelledByEmployeeId",
                table: "tbl_ContractAcceptanceRecord",
                column: "CancelledByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractAcceptanceRecord_ContractId_AcceptanceCode",
                table: "tbl_ContractAcceptanceRecord",
                columns: new[] { "ContractId", "AcceptanceCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractAcceptanceRecord_ContractVersionId_Status",
                table: "tbl_ContractAcceptanceRecord",
                columns: new[] { "ContractVersionId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractAcceptanceRecord_CreatedEmployeeId",
                table: "tbl_ContractAcceptanceRecord",
                column: "CreatedEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractAcceptanceRecord_FinalizedByEmployeeId",
                table: "tbl_ContractAcceptanceRecord",
                column: "FinalizedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractAcceptanceRecord_SignedByEmployeeId",
                table: "tbl_ContractAcceptanceRecord",
                column: "SignedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractAcceptanceRecord_TemplateVersionId",
                table: "tbl_ContractAcceptanceRecord",
                column: "TemplateVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractAcceptanceRecord_UpdatedEmployeeId",
                table: "tbl_ContractAcceptanceRecord",
                column: "UpdatedEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractAcceptanceReference_AcceptanceRecordId_DisplayOrder",
                table: "tbl_ContractAcceptanceReference",
                columns: new[] { "AcceptanceRecordId", "DisplayOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractAcceptanceReference_AcceptanceRecordId_ReferenceType_AppendixId",
                table: "tbl_ContractAcceptanceReference",
                columns: new[] { "AcceptanceRecordId", "ReferenceType", "AppendixId" },
                unique: true,
                filter: "[AppendixId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractAcceptanceReference_AppendixId",
                table: "tbl_ContractAcceptanceReference",
                column: "AppendixId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractAcceptanceSection_AcceptanceRecordId_DisplayOrder",
                table: "tbl_ContractAcceptanceSection",
                columns: new[] { "AcceptanceRecordId", "DisplayOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractAcceptanceSection_AcceptanceRecordId_SectionCode",
                table: "tbl_ContractAcceptanceSection",
                columns: new[] { "AcceptanceRecordId", "SectionCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractAppendixTerm_AppendixId_DisplayOrder",
                table: "tbl_ContractAppendixTerm",
                columns: new[] { "AppendixId", "DisplayOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractAppendixTerm_AppendixId_TermCode",
                table: "tbl_ContractAppendixTerm",
                columns: new[] { "AppendixId", "TermCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractAppendixTerm_SourceTemplateAppendixTermId",
                table: "tbl_ContractAppendixTerm",
                column: "SourceTemplateAppendixTermId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractTemplateAppendix_CreatedEmployeeId",
                table: "tbl_ContractTemplateAppendix",
                column: "CreatedEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractTemplateAppendix_TemplateVersionId_AppendixCode",
                table: "tbl_ContractTemplateAppendix",
                columns: new[] { "TemplateVersionId", "AppendixCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractTemplateAppendix_TemplateVersionId_DisplayOrder",
                table: "tbl_ContractTemplateAppendix",
                columns: new[] { "TemplateVersionId", "DisplayOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractTemplateAppendix_UpdatedEmployeeId",
                table: "tbl_ContractTemplateAppendix",
                column: "UpdatedEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractTemplateAppendixTerm_CreatedEmployeeId",
                table: "tbl_ContractTemplateAppendixTerm",
                column: "CreatedEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractTemplateAppendixTerm_TemplateAppendixId_DisplayOrder",
                table: "tbl_ContractTemplateAppendixTerm",
                columns: new[] { "TemplateAppendixId", "DisplayOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractTemplateAppendixTerm_TemplateAppendixId_TermCode",
                table: "tbl_ContractTemplateAppendixTerm",
                columns: new[] { "TemplateAppendixId", "TermCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractTemplateAppendixTerm_UpdatedEmployeeId",
                table: "tbl_ContractTemplateAppendixTerm",
                column: "UpdatedEmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_tbl_ContractAcceptanceEvidence_tbl_ContractAcceptanceRecord_AcceptanceRecordId",
                table: "tbl_ContractAcceptanceEvidence",
                column: "AcceptanceRecordId",
                principalTable: "tbl_ContractAcceptanceRecord",
                principalColumn: "AcceptanceRecordId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tbl_ContractAppendix_tbl_ContractTemplateAppendix_SourceTemplateAppendixId",
                table: "tbl_ContractAppendix",
                column: "SourceTemplateAppendixId",
                principalTable: "tbl_ContractTemplateAppendix",
                principalColumn: "TemplateAppendixId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tbl_ContractAppendix_tbl_ContractVersion_VersionId",
                table: "tbl_ContractAppendix",
                column: "VersionId",
                principalTable: "tbl_ContractVersion",
                principalColumn: "VersionId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tbl_ContractAppendix_tbl_Contract_ContractId",
                table: "tbl_ContractAppendix",
                column: "ContractId",
                principalTable: "tbl_Contract",
                principalColumn: "ContractId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tbl_ContractAppendix_tbl_Employee_CreatedEmployeeId",
                table: "tbl_ContractAppendix",
                column: "CreatedEmployeeId",
                principalTable: "tbl_Employee",
                principalColumn: "EmployeeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tbl_ContractAppendix_tbl_Employee_UpdatedEmployeeId",
                table: "tbl_ContractAppendix",
                column: "UpdatedEmployeeId",
                principalTable: "tbl_Employee",
                principalColumn: "EmployeeId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tbl_ContractAcceptanceEvidence_tbl_ContractAcceptanceRecord_AcceptanceRecordId",
                table: "tbl_ContractAcceptanceEvidence");

            migrationBuilder.DropForeignKey(
                name: "FK_tbl_ContractAppendix_tbl_ContractTemplateAppendix_SourceTemplateAppendixId",
                table: "tbl_ContractAppendix");

            migrationBuilder.DropForeignKey(
                name: "FK_tbl_ContractAppendix_tbl_ContractVersion_VersionId",
                table: "tbl_ContractAppendix");

            migrationBuilder.DropForeignKey(
                name: "FK_tbl_ContractAppendix_tbl_Contract_ContractId",
                table: "tbl_ContractAppendix");

            migrationBuilder.DropForeignKey(
                name: "FK_tbl_ContractAppendix_tbl_Employee_CreatedEmployeeId",
                table: "tbl_ContractAppendix");

            migrationBuilder.DropForeignKey(
                name: "FK_tbl_ContractAppendix_tbl_Employee_UpdatedEmployeeId",
                table: "tbl_ContractAppendix");

            migrationBuilder.DropTable(
                name: "tbl_ContractAcceptanceMilestone");

            migrationBuilder.DropTable(
                name: "tbl_ContractAcceptanceParty");

            migrationBuilder.DropTable(
                name: "tbl_ContractAcceptanceReference");

            migrationBuilder.DropTable(
                name: "tbl_ContractAcceptanceSection");

            migrationBuilder.DropTable(
                name: "tbl_ContractAppendixTerm");

            migrationBuilder.DropTable(
                name: "tbl_ContractAcceptanceRecord");

            migrationBuilder.DropTable(
                name: "tbl_ContractTemplateAppendixTerm");

            migrationBuilder.DropTable(
                name: "tbl_ContractTemplateAppendix");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractTemplateAuditValue_FieldCode",
                table: "tbl_ContractTemplateAuditValue");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractTemplateAuditValue_Value",
                table: "tbl_ContractTemplateAuditValue");

            migrationBuilder.DropIndex(
                name: "IX_tbl_ContractAppendix_ContractId_VersionId_DisplayOrder",
                table: "tbl_ContractAppendix");

            migrationBuilder.DropIndex(
                name: "IX_tbl_ContractAppendix_CreatedEmployeeId",
                table: "tbl_ContractAppendix");

            migrationBuilder.DropIndex(
                name: "IX_tbl_ContractAppendix_SourceTemplateAppendixId",
                table: "tbl_ContractAppendix");

            migrationBuilder.DropIndex(
                name: "IX_tbl_ContractAppendix_UpdatedEmployeeId",
                table: "tbl_ContractAppendix");

            migrationBuilder.DropIndex(
                name: "IX_tbl_ContractAppendix_VersionId_AppendixCode",
                table: "tbl_ContractAppendix");

            migrationBuilder.DropIndex(
                name: "IX_tbl_ContractAppendix_VersionId_DisplayOrder",
                table: "tbl_ContractAppendix");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractAppendix_Code",
                table: "tbl_ContractAppendix");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractAppendix_Order",
                table: "tbl_ContractAppendix");

            migrationBuilder.DropIndex(
                name: "IX_tbl_ContractAcceptanceEvidence_AcceptanceRecordId",
                table: "tbl_ContractAcceptanceEvidence");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractAcceptanceEvidence_RecordId",
                table: "tbl_ContractAcceptanceEvidence");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "tbl_ContractAppendix");

            migrationBuilder.DropColumn(
                name: "CreatedEmployeeId",
                table: "tbl_ContractAppendix");

            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "tbl_ContractAppendix");

            migrationBuilder.DropColumn(
                name: "IsRequired",
                table: "tbl_ContractAppendix");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "tbl_ContractAppendix");

            migrationBuilder.DropColumn(
                name: "SourceTemplateAppendixId",
                table: "tbl_ContractAppendix");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "tbl_ContractAppendix");

            migrationBuilder.DropColumn(
                name: "UpdatedEmployeeId",
                table: "tbl_ContractAppendix");

            migrationBuilder.DropColumn(
                name: "VersionId",
                table: "tbl_ContractAppendix");

            migrationBuilder.RenameColumn(
                name: "AcceptanceRecordId",
                table: "tbl_ContractAcceptanceEvidence",
                newName: "VersionId");

            migrationBuilder.AlterColumn<string>(
                name: "AppendixNameEn",
                table: "tbl_ContractAppendix",
                type: "varchar(500)",
                unicode: false,
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AppendixName",
                table: "tbl_ContractAppendix",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "AppendixCode",
                table: "tbl_ContractAppendix",
                type: "varchar(50)",
                unicode: false,
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldUnicode: false,
                oldMaxLength: 50);

            migrationBuilder.AddColumn<DateTime>(
                name: "AppendixDate",
                table: "tbl_ContractAppendix",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ContractId",
                table: "tbl_ContractAcceptanceEvidence",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractTemplateAuditValue_FieldCode",
                table: "tbl_ContractTemplateAuditValue",
                sql: "[FieldCode] BETWEEN 1 AND 11");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractTemplateAuditValue_Value",
                table: "tbl_ContractTemplateAuditValue",
                sql: "([IsNull] = 1 AND [IntegerValue] IS NULL AND [LongValue] IS NULL AND [StringValue] IS NULL) OR ([IsNull] = 0 AND (([FieldCode] IN (1, 5, 6, 9) AND [IntegerValue] IS NOT NULL AND [LongValue] IS NULL AND [StringValue] IS NULL) OR ([FieldCode] IN (3, 7, 10) AND [IntegerValue] IS NULL AND [LongValue] IS NOT NULL AND [StringValue] IS NULL) OR ([FieldCode] IN (2, 4, 8, 11) AND [IntegerValue] IS NULL AND [LongValue] IS NULL AND [StringValue] IS NOT NULL)))");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractAcceptanceEvidence_ContractId_VersionId",
                table: "tbl_ContractAcceptanceEvidence",
                columns: new[] { "ContractId", "VersionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractAcceptanceEvidence_VersionId",
                table: "tbl_ContractAcceptanceEvidence",
                column: "VersionId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractAcceptanceEvidence_ContractId",
                table: "tbl_ContractAcceptanceEvidence",
                sql: "[ContractId] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractAcceptanceEvidence_VersionId",
                table: "tbl_ContractAcceptanceEvidence",
                sql: "[VersionId] > 0");

            migrationBuilder.AddForeignKey(
                name: "FK_tbl_ContractAcceptanceEvidence_tbl_ContractVersion_VersionId",
                table: "tbl_ContractAcceptanceEvidence",
                column: "VersionId",
                principalTable: "tbl_ContractVersion",
                principalColumn: "VersionId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tbl_ContractAcceptanceEvidence_tbl_Contract_ContractId",
                table: "tbl_ContractAcceptanceEvidence",
                column: "ContractId",
                principalTable: "tbl_Contract",
                principalColumn: "ContractId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
