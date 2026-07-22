using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractManagement.Infrastructure.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddContractItemSnapshotTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tbl_ContractItem",
                columns: table => new
                {
                    ContractItemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractId = table.Column<int>(type: "int", nullable: false),
                    VersionId = table.Column<int>(type: "int", nullable: false),
                    ItemType = table.Column<byte>(type: "tinyint", nullable: false),
                    SourceProductId = table.Column<int>(type: "int", nullable: true),
                    SourceServiceId = table.Column<int>(type: "int", nullable: true),
                    ItemCode = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    ItemName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ItemNameEn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ItemDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ItemDescriptionEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UnitName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UnitNameEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 1m)
                        .Annotation("Relational:DefaultConstraintName", "DF_tbl_ContractItem_Quantity"),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LineSubtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 0m)
                        .Annotation("Relational:DefaultConstraintName", "DF_tbl_ContractItem_DiscountPercent"),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m)
                        .Annotation("Relational:DefaultConstraintName", "DF_tbl_ContractItem_DiscountAmount"),
                    VatPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 0m)
                        .Annotation("Relational:DefaultConstraintName", "DF_tbl_ContractItem_VatPercent"),
                    VatAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m)
                        .Annotation("Relational:DefaultConstraintName", "DF_tbl_ContractItem_VatAmount"),
                    LineTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                        .Annotation("Relational:DefaultConstraintName", "DF_tbl_ContractItem_DisplayOrder"),
                    CreatedEmployeeId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysutcdatetime())")
                        .Annotation("Relational:DefaultConstraintName", "DF_tbl_ContractItem_CreatedDate"),
                    UpdatedEmployeeId = table.Column<int>(type: "int", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_ContractItem", x => x.ContractItemId);
                    table.CheckConstraint("CK_tbl_ContractItem_ContractId", "[ContractId] > 0");
                    table.CheckConstraint("CK_tbl_ContractItem_CreatedEmployeeId", "[CreatedEmployeeId] > 0");
                    table.CheckConstraint("CK_tbl_ContractItem_DiscountAmount", "[DiscountAmount] >= 0 AND [DiscountAmount] <= [LineSubtotal]");
                    table.CheckConstraint("CK_tbl_ContractItem_DiscountPercent", "[DiscountPercent] >= 0 AND [DiscountPercent] <= 100");
                    table.CheckConstraint("CK_tbl_ContractItem_DisplayOrder", "[DisplayOrder] >= 0");
                    table.CheckConstraint("CK_tbl_ContractItem_ItemName", "LEN(LTRIM(RTRIM([ItemName]))) > 0");
                    table.CheckConstraint("CK_tbl_ContractItem_ItemType", "[ItemType] IN (1, 2)");
                    table.CheckConstraint("CK_tbl_ContractItem_LineSubtotal", "[LineSubtotal] >= 0");
                    table.CheckConstraint("CK_tbl_ContractItem_LineTotal", "[LineTotal] >= 0");
                    table.CheckConstraint("CK_tbl_ContractItem_Quantity", "[Quantity] > 0");
                    table.CheckConstraint("CK_tbl_ContractItem_SourceByType", "([ItemType] = 1 AND [SourceServiceId] IS NULL) OR ([ItemType] = 2 AND [SourceProductId] IS NULL)");
                    table.CheckConstraint("CK_tbl_ContractItem_SourceProductId", "[SourceProductId] IS NULL OR [SourceProductId] > 0");
                    table.CheckConstraint("CK_tbl_ContractItem_SourceServiceId", "[SourceServiceId] IS NULL OR [SourceServiceId] > 0");
                    table.CheckConstraint("CK_tbl_ContractItem_UnitPrice", "[UnitPrice] >= 0");
                    table.CheckConstraint("CK_tbl_ContractItem_UpdatedEmployeeId", "[UpdatedEmployeeId] IS NULL OR [UpdatedEmployeeId] > 0");
                    table.CheckConstraint("CK_tbl_ContractItem_VatAmount", "[VatAmount] >= 0");
                    table.CheckConstraint("CK_tbl_ContractItem_VatPercent", "[VatPercent] >= 0 AND [VatPercent] <= 100");
                    table.CheckConstraint("CK_tbl_ContractItem_VersionId", "[VersionId] > 0");
                });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractItem_Contract_Version",
                table: "tbl_ContractItem",
                columns: new[] { "ContractId", "VersionId" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractItem_Version_DisplayOrder",
                table: "tbl_ContractItem",
                columns: new[] { "VersionId", "DisplayOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbl_ContractItem");
        }
    }
}
