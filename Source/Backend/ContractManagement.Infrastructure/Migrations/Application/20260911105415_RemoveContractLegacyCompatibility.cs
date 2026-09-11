using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractManagement.Infrastructure.Migrations.Application
{
    /// <inheritdoc />
    public partial class RemoveContractLegacyCompatibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractVersion_TemplateVersionId",
                table: "tbl_ContractVersion");

            migrationBuilder.DropColumn(
                name: "IsLegacy",
                table: "tbl_Contract");

            migrationBuilder.AlterColumn<int>(
                name: "TemplateVersionId",
                table: "tbl_ContractVersion",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SourceFieldKey",
                table: "tbl_ContractTemplateField",
                type: "varchar(200)",
                unicode: false,
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldUnicode: false,
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "TemplateVersionId",
                table: "tbl_Contract",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractVersion_TemplateVersionId",
                table: "tbl_ContractVersion",
                sql: "[TemplateVersionId] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_Contract_TemplateVersionId",
                table: "tbl_Contract",
                sql: "[TemplateVersionId] > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractVersion_TemplateVersionId",
                table: "tbl_ContractVersion");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_Contract_TemplateVersionId",
                table: "tbl_Contract");

            migrationBuilder.AlterColumn<int>(
                name: "TemplateVersionId",
                table: "tbl_ContractVersion",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "SourceFieldKey",
                table: "tbl_ContractTemplateField",
                type: "varchar(200)",
                unicode: false,
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldUnicode: false,
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<int>(
                name: "TemplateVersionId",
                table: "tbl_Contract",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<bool>(
                name: "IsLegacy",
                table: "tbl_Contract",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractVersion_TemplateVersionId",
                table: "tbl_ContractVersion",
                sql: "[TemplateVersionId] IS NULL OR [TemplateVersionId] > 0");
        }
    }
}
