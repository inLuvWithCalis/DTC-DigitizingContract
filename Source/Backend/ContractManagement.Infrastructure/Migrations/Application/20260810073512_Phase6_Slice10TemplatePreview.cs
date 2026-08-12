using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractManagement.Infrastructure.Migrations.Application
{
    /// <inheritdoc />
    public partial class Phase6_Slice10TemplatePreview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PreviewFileId",
                table: "tbl_ContractTemplateVersion",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreviewSourceHash",
                table: "tbl_ContractTemplateVersion",
                type: "char(64)",
                unicode: false,
                fixedLength: true,
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PreviewedAt",
                table: "tbl_ContractTemplateVersion",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PreviewedByEmployeeId",
                table: "tbl_ContractTemplateVersion",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "UX_tbl_ContractTemplateVersion_PreviewFileId",
                table: "tbl_ContractTemplateVersion",
                column: "PreviewFileId",
                unique: true,
                filter: "[PreviewFileId] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractTemplateVersion_Preview",
                table: "tbl_ContractTemplateVersion",
                sql: "([PreviewSourceHash] IS NULL AND [PreviewedAt] IS NULL AND [PreviewedByEmployeeId] IS NULL AND [PreviewFileId] IS NULL) OR ([PreviewSourceHash] IS NOT NULL AND LEN([PreviewSourceHash]) = 64 AND [PreviewedAt] IS NOT NULL AND [PreviewedByEmployeeId] > 0 AND ([PreviewFileId] IS NULL OR [PreviewFileId] > 0))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_tbl_ContractTemplateVersion_PreviewFileId",
                table: "tbl_ContractTemplateVersion");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractTemplateVersion_Preview",
                table: "tbl_ContractTemplateVersion");

            migrationBuilder.DropColumn(
                name: "PreviewFileId",
                table: "tbl_ContractTemplateVersion");

            migrationBuilder.DropColumn(
                name: "PreviewSourceHash",
                table: "tbl_ContractTemplateVersion");

            migrationBuilder.DropColumn(
                name: "PreviewedAt",
                table: "tbl_ContractTemplateVersion");

            migrationBuilder.DropColumn(
                name: "PreviewedByEmployeeId",
                table: "tbl_ContractTemplateVersion");
        }
    }
}
