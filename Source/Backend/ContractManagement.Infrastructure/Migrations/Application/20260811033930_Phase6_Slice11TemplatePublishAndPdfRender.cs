using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractManagement.Infrastructure.Migrations.Application
{
    /// <inheritdoc />
    public partial class Phase6_Slice11TemplatePublishAndPdfRender : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractTemplateVersion_PublishState",
                table: "tbl_ContractTemplateVersion");

            migrationBuilder.AddColumn<int>(
                name: "PublishedPreviewPdfFileId",
                table: "tbl_ContractTemplateVersion",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "UX_tbl_ContractTemplateVersion_PublishedPreviewPdfFileId",
                table: "tbl_ContractTemplateVersion",
                column: "PublishedPreviewPdfFileId",
                unique: true,
                filter: "[PublishedPreviewPdfFileId] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractTemplateVersion_PublishState",
                table: "tbl_ContractTemplateVersion",
                sql: "([Status] = 0 AND [PublishedByEmployeeId] IS NULL AND [PublishedDate] IS NULL AND [PublishedPreviewPdfFileId] IS NULL) OR ([Status] IN (1, 2) AND [ValidationStatus] = 1 AND [DocumentFileId] IS NOT NULL AND [DocumentHash] IS NOT NULL AND [PreviewFileId] IS NOT NULL AND [PublishedPreviewPdfFileId] > 0 AND [PublishedByEmployeeId] IS NOT NULL AND [PublishedDate] IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_tbl_ContractTemplateVersion_PublishedPreviewPdfFileId",
                table: "tbl_ContractTemplateVersion");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractTemplateVersion_PublishState",
                table: "tbl_ContractTemplateVersion");

            migrationBuilder.DropColumn(
                name: "PublishedPreviewPdfFileId",
                table: "tbl_ContractTemplateVersion");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractTemplateVersion_PublishState",
                table: "tbl_ContractTemplateVersion",
                sql: "([Status] = 0 AND [PublishedByEmployeeId] IS NULL AND [PublishedDate] IS NULL) OR ([Status] IN (1, 2) AND [ValidationStatus] = 1 AND [DocumentFileId] IS NOT NULL AND [DocumentHash] IS NOT NULL AND [PublishedByEmployeeId] IS NOT NULL AND [PublishedDate] IS NOT NULL)");
        }
    }
}
