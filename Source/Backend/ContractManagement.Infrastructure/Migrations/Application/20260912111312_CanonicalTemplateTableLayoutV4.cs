using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractManagement.Infrastructure.Migrations.Application
{
    /// <inheritdoc />
    public partial class CanonicalTemplateTableLayoutV4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tbl_ContractTemplateItemTableColumnLayout",
                columns: table => new
                {
                    ItemTableColumnLayoutId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateVersionId = table.Column<int>(type: "int", nullable: false),
                    ColumnKey = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    DisplayOrder = table.Column<byte>(type: "tinyint", nullable: false),
                    WidthBps = table.Column<short>(type: "smallint", nullable: false),
                    CreatedEmployeeId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    UpdatedEmployeeId = table.Column<int>(type: "int", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_ContractTemplateItemTableColumnLayout", x => x.ItemTableColumnLayoutId);
                    table.CheckConstraint("CK_tbl_ContractTemplateItemTableColumnLayout_DisplayOrder", "[DisplayOrder] BETWEEN 0 AND 7");
                    table.CheckConstraint("CK_tbl_ContractTemplateItemTableColumnLayout_WidthBps", "[WidthBps] BETWEEN 250 AND 10000");
                    table.ForeignKey(
                        name: "FK_tbl_ContractTemplateItemTableColumnLayout_tbl_ContractTemplateVersion_TemplateVersionId",
                        column: x => x.TemplateVersionId,
                        principalTable: "tbl_ContractTemplateVersion",
                        principalColumn: "TemplateVersionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "UX_tbl_ContractTemplateItemTableColumnLayout_Version_Key",
                table: "tbl_ContractTemplateItemTableColumnLayout",
                columns: new[] { "TemplateVersionId", "ColumnKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_tbl_ContractTemplateItemTableColumnLayout_Version_Order",
                table: "tbl_ContractTemplateItemTableColumnLayout",
                columns: new[] { "TemplateVersionId", "DisplayOrder" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbl_ContractTemplateItemTableColumnLayout");
        }
    }
}
