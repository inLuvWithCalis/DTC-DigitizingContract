using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractManagement.Infrastructure.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddContractTemplateFieldAndTermTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tbl_ContractTemplateField",
                columns: table => new
                {
                    TemplateFieldId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateVersionId = table.Column<int>(type: "int", nullable: false),
                    PlaceholderKey = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    FieldLabel = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    DataSource = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: false),
                    DefaultValue = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    FormatString = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                        .Annotation("Relational:DefaultConstraintName", "DF_tbl_ContractTemplateField_IsRequired"),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                        .Annotation("Relational:DefaultConstraintName", "DF_tbl_ContractTemplateField_DisplayOrder"),
                    CreatedEmployeeId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysutcdatetime())")
                        .Annotation("Relational:DefaultConstraintName", "DF_tbl_ContractTemplateField_CreatedDate"),
                    UpdatedEmployeeId = table.Column<int>(type: "int", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_ContractTemplateField", x => x.TemplateFieldId);
                    table.CheckConstraint("CK_tbl_ContractTemplateField_DataSource", "LEN(LTRIM(RTRIM([DataSource]))) > 0");
                    table.CheckConstraint("CK_tbl_ContractTemplateField_DisplayOrder", "[DisplayOrder] >= 0");
                    table.CheckConstraint("CK_tbl_ContractTemplateField_PlaceholderKey", "LEN(LTRIM(RTRIM([PlaceholderKey]))) > 0");
                    table.CheckConstraint("CK_tbl_ContractTemplateField_TemplateVersionId", "[TemplateVersionId] > 0");
                });

            migrationBuilder.CreateTable(
                name: "tbl_ContractTemplateTerm",
                columns: table => new
                {
                    TemplateTermId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateVersionId = table.Column<int>(type: "int", nullable: false),
                    TermCode = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    TermTitle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TermTitleEn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TermContent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TermContentEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsNegotiable = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                        .Annotation("Relational:DefaultConstraintName", "DF_tbl_ContractTemplateTerm_IsNegotiable"),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                        .Annotation("Relational:DefaultConstraintName", "DF_tbl_ContractTemplateTerm_DisplayOrder"),
                    CreatedEmployeeId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysutcdatetime())")
                        .Annotation("Relational:DefaultConstraintName", "DF_tbl_ContractTemplateTerm_CreatedDate"),
                    UpdatedEmployeeId = table.Column<int>(type: "int", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_ContractTemplateTerm", x => x.TemplateTermId);
                    table.CheckConstraint("CK_tbl_ContractTemplateTerm_DisplayOrder", "[DisplayOrder] >= 0");
                    table.CheckConstraint("CK_tbl_ContractTemplateTerm_TemplateVersionId", "[TemplateVersionId] > 0");
                    table.CheckConstraint("CK_tbl_ContractTemplateTerm_TermCode", "LEN(LTRIM(RTRIM([TermCode]))) > 0");
                });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractTemplateField_Version_DisplayOrder",
                table: "tbl_ContractTemplateField",
                columns: new[] { "TemplateVersionId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "UX_tbl_ContractTemplateField_Version_Placeholder",
                table: "tbl_ContractTemplateField",
                columns: new[] { "TemplateVersionId", "PlaceholderKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractTemplateTerm_Version_DisplayOrder",
                table: "tbl_ContractTemplateTerm",
                columns: new[] { "TemplateVersionId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "UX_tbl_ContractTemplateTerm_Version_TermCode",
                table: "tbl_ContractTemplateTerm",
                columns: new[] { "TemplateVersionId", "TermCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbl_ContractTemplateField");

            migrationBuilder.DropTable(
                name: "tbl_ContractTemplateTerm");
        }
    }
}
