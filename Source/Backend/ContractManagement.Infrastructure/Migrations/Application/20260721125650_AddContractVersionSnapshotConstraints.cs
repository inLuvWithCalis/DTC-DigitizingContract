using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractManagement.Infrastructure.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddContractVersionSnapshotConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractVersion_ContractId",
                table: "tbl_ContractVersion",
                sql: "[ContractId] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractVersion_SourceVersionId",
                table: "tbl_ContractVersion",
                sql: "[SourceVersionId] IS NULL OR [SourceVersionId] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractVersion_TemplateVersionId",
                table: "tbl_ContractVersion",
                sql: "[TemplateVersionId] IS NULL OR [TemplateVersionId] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractTerm_ContractId",
                table: "tbl_ContractTerm",
                sql: "[ContractId] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractTerm_SourceTemplateTermId",
                table: "tbl_ContractTerm",
                sql: "[SourceTemplateTermId] IS NULL OR [SourceTemplateTermId] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractTerm_VersionId",
                table: "tbl_ContractTerm",
                sql: "[VersionId] > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractVersion_ContractId",
                table: "tbl_ContractVersion");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractVersion_SourceVersionId",
                table: "tbl_ContractVersion");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractVersion_TemplateVersionId",
                table: "tbl_ContractVersion");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractTerm_ContractId",
                table: "tbl_ContractTerm");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractTerm_SourceTemplateTermId",
                table: "tbl_ContractTerm");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractTerm_VersionId",
                table: "tbl_ContractTerm");
        }
    }
}
