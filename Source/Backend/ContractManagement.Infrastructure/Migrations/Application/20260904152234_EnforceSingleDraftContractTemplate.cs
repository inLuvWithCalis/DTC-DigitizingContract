using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractManagement.Infrastructure.Migrations.Application
{
    /// <inheritdoc />
    public partial class EnforceSingleDraftContractTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS (
                    SELECT 1
                    FROM [tbl_ContractTemplateVersion]
                    WHERE [Status] = 0
                    GROUP BY [TemplateId]
                    HAVING COUNT(*) > 1
                )
                BEGIN
                    THROW 51000, N'Không thể áp dụng ràng buộc một Draft cho mỗi template vì đang có template chứa nhiều Draft. Hãy chọn Draft cần giữ và xử lý các Draft còn lại trước khi chạy lại migration.', 1;
                END
                """);

            migrationBuilder.CreateIndex(
                name: "UX_tbl_ContractTemplateVersion_OneDraftPerTemplate",
                table: "tbl_ContractTemplateVersion",
                column: "TemplateId",
                unique: true,
                filter: "[Status] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_tbl_ContractTemplateVersion_OneDraftPerTemplate",
                table: "tbl_ContractTemplateVersion");
        }
    }
}
