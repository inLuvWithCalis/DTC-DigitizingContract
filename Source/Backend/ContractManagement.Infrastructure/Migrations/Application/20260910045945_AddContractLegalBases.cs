using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractManagement.Infrastructure.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddContractLegalBases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tbl_ContractLegalBasis",
                columns: table => new
                {
                    LegalBasisId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractId = table.Column<int>(type: "int", nullable: false),
                    VersionId = table.Column<int>(type: "int", nullable: false),
                    SourceTemplateLegalBasisId = table.Column<int>(type: "int", nullable: true),
                    BasisCode = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    ContentVi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                        .Annotation("Relational:DefaultConstraintName", "DF_tbl_ContractLegalBasis_DisplayOrder"),
                    CreatedEmployeeId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysutcdatetime())")
                        .Annotation("Relational:DefaultConstraintName", "DF_tbl_ContractLegalBasis_CreatedDate"),
                    UpdatedEmployeeId = table.Column<int>(type: "int", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_ContractLegalBasis", x => x.LegalBasisId);
                    table.CheckConstraint("CK_tbl_ContractLegalBasis_BasisCode", "LEN(LTRIM(RTRIM([BasisCode]))) > 0");
                    table.CheckConstraint("CK_tbl_ContractLegalBasis_ContentVi", "LEN(LTRIM(RTRIM([ContentVi]))) > 0");
                    table.CheckConstraint("CK_tbl_ContractLegalBasis_ContractId", "[ContractId] > 0");
                    table.CheckConstraint("CK_tbl_ContractLegalBasis_DisplayOrder", "[DisplayOrder] >= 0");
                    table.CheckConstraint("CK_tbl_ContractLegalBasis_SourceTemplateLegalBasisId", "[SourceTemplateLegalBasisId] IS NULL OR [SourceTemplateLegalBasisId] > 0");
                    table.CheckConstraint("CK_tbl_ContractLegalBasis_VersionId", "[VersionId] > 0");
                });

            migrationBuilder.CreateTable(
                name: "tbl_ContractTemplateLegalBasis",
                columns: table => new
                {
                    TemplateLegalBasisId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateVersionId = table.Column<int>(type: "int", nullable: false),
                    BasisCode = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    ContentVi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                        .Annotation("Relational:DefaultConstraintName", "DF_tbl_ContractTemplateLegalBasis_DisplayOrder"),
                    CreatedEmployeeId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysutcdatetime())")
                        .Annotation("Relational:DefaultConstraintName", "DF_tbl_ContractTemplateLegalBasis_CreatedDate"),
                    UpdatedEmployeeId = table.Column<int>(type: "int", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_ContractTemplateLegalBasis", x => x.TemplateLegalBasisId);
                    table.CheckConstraint("CK_tbl_ContractTemplateLegalBasis_BasisCode", "LEN(LTRIM(RTRIM([BasisCode]))) > 0");
                    table.CheckConstraint("CK_tbl_ContractTemplateLegalBasis_ContentVi", "LEN(LTRIM(RTRIM([ContentVi]))) > 0");
                    table.CheckConstraint("CK_tbl_ContractTemplateLegalBasis_DisplayOrder", "[DisplayOrder] >= 0");
                    table.CheckConstraint("CK_tbl_ContractTemplateLegalBasis_TemplateVersionId", "[TemplateVersionId] > 0");
                });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractLegalBasis_Contract_Version_DisplayOrder",
                table: "tbl_ContractLegalBasis",
                columns: new[] { "ContractId", "VersionId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractLegalBasis_SourceTemplateLegalBasisId",
                table: "tbl_ContractLegalBasis",
                column: "SourceTemplateLegalBasisId");

            migrationBuilder.CreateIndex(
                name: "UX_tbl_ContractLegalBasis_Version_BasisCode",
                table: "tbl_ContractLegalBasis",
                columns: new[] { "VersionId", "BasisCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractTemplateLegalBasis_Version_DisplayOrder",
                table: "tbl_ContractTemplateLegalBasis",
                columns: new[] { "TemplateVersionId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "UX_tbl_ContractTemplateLegalBasis_Version_BasisCode",
                table: "tbl_ContractTemplateLegalBasis",
                columns: new[] { "TemplateVersionId", "BasisCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbl_ContractLegalBasis");

            migrationBuilder.DropTable(
                name: "tbl_ContractTemplateLegalBasis");
        }
    }
}
