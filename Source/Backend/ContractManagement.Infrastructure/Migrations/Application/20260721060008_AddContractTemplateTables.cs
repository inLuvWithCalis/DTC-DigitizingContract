using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractManagement.Infrastructure.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddContractTemplateTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tbl_ContractTemplate",
                columns: table => new
                {
                    TemplateId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateCode = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    TemplateName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TemplateNameEn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DocumentType = table.Column<byte>(type: "tinyint", nullable: false),
                    LanguageMode = table.Column<byte>(type: "tinyint", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CurrentPublishedVersionId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                        .Annotation("Relational:DefaultConstraintName", "DF_tbl_ContractTemplate_IsActive"),
                    CreatedEmployeeId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysutcdatetime())")
                        .Annotation("Relational:DefaultConstraintName", "DF_tbl_ContractTemplate_CreatedDate"),
                    UpdatedEmployeeId = table.Column<int>(type: "int", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_ContractTemplate", x => x.TemplateId);
                    table.CheckConstraint("CK_tbl_ContractTemplate_CurrentPublishedVersionId", "[CurrentPublishedVersionId] IS NULL OR [CurrentPublishedVersionId] > 0");
                    table.CheckConstraint("CK_tbl_ContractTemplate_DocumentType", "[DocumentType] IN (1, 2, 3, 4, 5, 6, 7, 8, 99)");
                    table.CheckConstraint("CK_tbl_ContractTemplate_LanguageMode", "[LanguageMode] IN (1, 2)");
                    table.CheckConstraint("CK_tbl_ContractTemplate_TemplateCode", "LEN(LTRIM(RTRIM([TemplateCode]))) > 0");
                });

            migrationBuilder.CreateTable(
                name: "tbl_ContractTemplateVersion",
                columns: table => new
                {
                    TemplateVersionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateId = table.Column<int>(type: "int", nullable: false),
                    VersionNo = table.Column<int>(type: "int", nullable: false),
                    ChangeNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)0)
                        .Annotation("Relational:DefaultConstraintName", "DF_tbl_ContractTemplateVersion_Status"),
                    ValidationStatus = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)0)
                        .Annotation("Relational:DefaultConstraintName", "DF_tbl_ContractTemplateVersion_ValidationStatus"),
                    ValidationMessage = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    DocumentFileId = table.Column<int>(type: "int", nullable: true),
                    DocumentHash = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: true),
                    ValidatedByEmployeeId = table.Column<int>(type: "int", nullable: true),
                    ValidatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PublishedByEmployeeId = table.Column<int>(type: "int", nullable: true),
                    PublishedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedEmployeeId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysutcdatetime())")
                        .Annotation("Relational:DefaultConstraintName", "DF_tbl_ContractTemplateVersion_CreatedDate"),
                    UpdatedEmployeeId = table.Column<int>(type: "int", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_ContractTemplateVersion", x => x.TemplateVersionId);
                    table.CheckConstraint("CK_tbl_ContractTemplateVersion_Document", "([DocumentFileId] IS NULL AND [DocumentHash] IS NULL) OR ([DocumentFileId] > 0 AND [DocumentHash] IS NOT NULL AND LEN([DocumentHash]) = 64)");
                    table.CheckConstraint("CK_tbl_ContractTemplateVersion_PublishState", "([Status] = 0 AND [PublishedByEmployeeId] IS NULL AND [PublishedDate] IS NULL) OR ([Status] IN (1, 2) AND [ValidationStatus] = 1 AND [DocumentFileId] IS NOT NULL AND [DocumentHash] IS NOT NULL AND [PublishedByEmployeeId] IS NOT NULL AND [PublishedDate] IS NOT NULL)");
                    table.CheckConstraint("CK_tbl_ContractTemplateVersion_Status", "[Status] IN (0, 1, 2)");
                    table.CheckConstraint("CK_tbl_ContractTemplateVersion_Validation", "([ValidationStatus] = 0 AND [ValidatedByEmployeeId] IS NULL AND [ValidatedDate] IS NULL AND [ValidationMessage] IS NULL) OR ([ValidationStatus] = 1 AND [ValidatedByEmployeeId] IS NOT NULL AND [ValidatedDate] IS NOT NULL) OR ([ValidationStatus] = 2 AND [ValidatedByEmployeeId] IS NOT NULL AND [ValidatedDate] IS NOT NULL AND LEN(LTRIM(RTRIM(COALESCE([ValidationMessage], N'')))) > 0)");
                    table.CheckConstraint("CK_tbl_ContractTemplateVersion_ValidationStatus", "[ValidationStatus] IN (0, 1, 2)");
                    table.CheckConstraint("CK_tbl_ContractTemplateVersion_VersionNo", "[VersionNo] > 0");
                });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractTemplate_CurrentPublishedVersionId",
                table: "tbl_ContractTemplate",
                column: "CurrentPublishedVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractTemplate_DocumentType_IsActive",
                table: "tbl_ContractTemplate",
                columns: new[] { "DocumentType", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "UX_tbl_ContractTemplate_TemplateCode",
                table: "tbl_ContractTemplate",
                column: "TemplateCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractTemplateVersion_TemplateId_Status",
                table: "tbl_ContractTemplateVersion",
                columns: new[] { "TemplateId", "Status" });

            migrationBuilder.CreateIndex(
                name: "UX_tbl_ContractTemplateVersion_DocumentFileId",
                table: "tbl_ContractTemplateVersion",
                column: "DocumentFileId",
                unique: true,
                filter: "[DocumentFileId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_tbl_ContractTemplateVersion_TemplateId_VersionNo",
                table: "tbl_ContractTemplateVersion",
                columns: new[] { "TemplateId", "VersionNo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbl_ContractTemplate");

            migrationBuilder.DropTable(
                name: "tbl_ContractTemplateVersion");
        }
    }
}
