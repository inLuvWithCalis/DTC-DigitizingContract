using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractManagement.Infrastructure.Migrations.Application
{
    /// <inheritdoc />
    public partial class Phase6_Slice04SnapshotFinanceVersionTermFoundations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CurrencyCode",
                table: "tbl_ContractVersion",
                type: "char(3)",
                unicode: false,
                fixedLength: true,
                maxLength: 3,
                nullable: false,
                defaultValue: "VND");

            migrationBuilder.AddColumn<decimal>(
                name: "Subtotal",
                table: "tbl_ContractVersion",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalAmount",
                table: "tbl_ContractVersion",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalDiscount",
                table: "tbl_ContractVersion",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalVat",
                table: "tbl_ContractVersion",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<byte>(
                name: "DiscountMode",
                table: "tbl_ContractItem",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0)
                .Annotation("Relational:DefaultConstraintName", "DF_tbl_ContractItem_DiscountMode");

            migrationBuilder.AddColumn<decimal>(
                name: "FixedDiscountAmount",
                table: "tbl_ContractItem",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m)
                .Annotation("Relational:DefaultConstraintName", "DF_tbl_ContractItem_FixedDiscountAmount");

            migrationBuilder.AddColumn<bool>(
                name: "IsTaxable",
                table: "tbl_ContractItem",
                type: "bit",
                nullable: false,
                defaultValue: true)
                .Annotation("Relational:DefaultConstraintName", "DF_tbl_ContractItem_IsTaxable");

            migrationBuilder.AddColumn<decimal>(
                name: "Subtotal",
                table: "tbl_Contract",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalDiscount",
                table: "tbl_Contract",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalVat",
                table: "tbl_Contract",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql(
                """
                UPDATE [tbl_ContractItem]
                SET [DiscountMode] = 1
                WHERE [DiscountPercent] > 0;

                UPDATE c
                SET
                    [Subtotal] = totals.[Subtotal],
                    [TotalDiscount] = totals.[TotalDiscount],
                    [TotalVat] = totals.[TotalVat],
                    [TotalAmount] = totals.[TotalAmount]
                FROM [tbl_Contract] c
                CROSS APPLY
                (
                    SELECT
                        COALESCE(SUM(i.[LineSubtotal]), 0) AS [Subtotal],
                        COALESCE(SUM(i.[DiscountAmount]), 0) AS [TotalDiscount],
                        COALESCE(SUM(i.[VatAmount]), 0) AS [TotalVat],
                        COALESCE(SUM(i.[LineTotal]), 0) AS [TotalAmount]
                    FROM [tbl_ContractItem] i
                    WHERE i.[ContractId] = c.[ContractId]
                      AND i.[VersionId] = c.[CurrentVersionId]
                ) totals;

                UPDATE v
                SET
                    [CurrencyCode] = c.[CurrencyCode],
                    [Subtotal] = totals.[Subtotal],
                    [TotalDiscount] = totals.[TotalDiscount],
                    [TotalVat] = totals.[TotalVat],
                    [TotalAmount] = totals.[TotalAmount]
                FROM [tbl_ContractVersion] v
                INNER JOIN [tbl_Contract] c
                    ON c.[ContractId] = v.[ContractId]
                CROSS APPLY
                (
                    SELECT
                        COALESCE(SUM(i.[LineSubtotal]), 0) AS [Subtotal],
                        COALESCE(SUM(i.[DiscountAmount]), 0) AS [TotalDiscount],
                        COALESCE(SUM(i.[VatAmount]), 0) AS [TotalVat],
                        COALESCE(SUM(i.[LineTotal]), 0) AS [TotalAmount]
                    FROM [tbl_ContractItem] i
                    WHERE i.[ContractId] = v.[ContractId]
                      AND i.[VersionId] = v.[VersionId]
                ) totals;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractVersion_CurrencyCode",
                table: "tbl_ContractVersion",
                sql: "[CurrencyCode] IN ('VND', 'USD')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractVersion_FinancialTotals",
                table: "tbl_ContractVersion",
                sql: "[Subtotal] >= 0 AND [TotalDiscount] >= 0 AND [TotalVat] >= 0 AND [TotalAmount] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractItem_DiscountInputs",
                table: "tbl_ContractItem",
                sql: "([DiscountMode] = 0 AND [DiscountPercent] = 0 AND [FixedDiscountAmount] = 0) OR ([DiscountMode] = 1 AND [FixedDiscountAmount] = 0) OR ([DiscountMode] = 2 AND [DiscountPercent] = 0 AND [FixedDiscountAmount] <= [LineSubtotal])");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractItem_DiscountMode",
                table: "tbl_ContractItem",
                sql: "[DiscountMode] IN (0, 1, 2)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractItem_TaxableVat",
                table: "tbl_ContractItem",
                sql: "[IsTaxable] = 1 OR ([VatPercent] = 0 AND [VatAmount] = 0)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_Contract_FinancialTotals",
                table: "tbl_Contract",
                sql: "[Subtotal] >= 0 AND [TotalDiscount] >= 0 AND [TotalVat] >= 0 AND [TotalAmount] >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractVersion_CurrencyCode",
                table: "tbl_ContractVersion");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractVersion_FinancialTotals",
                table: "tbl_ContractVersion");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractItem_DiscountInputs",
                table: "tbl_ContractItem");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractItem_DiscountMode",
                table: "tbl_ContractItem");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractItem_TaxableVat",
                table: "tbl_ContractItem");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_Contract_FinancialTotals",
                table: "tbl_Contract");

            migrationBuilder.DropColumn(
                name: "CurrencyCode",
                table: "tbl_ContractVersion");

            migrationBuilder.DropColumn(
                name: "Subtotal",
                table: "tbl_ContractVersion");

            migrationBuilder.DropColumn(
                name: "TotalAmount",
                table: "tbl_ContractVersion");

            migrationBuilder.DropColumn(
                name: "TotalDiscount",
                table: "tbl_ContractVersion");

            migrationBuilder.DropColumn(
                name: "TotalVat",
                table: "tbl_ContractVersion");

            migrationBuilder.DropColumn(
                name: "DiscountMode",
                table: "tbl_ContractItem")
                .Annotation("Relational:DefaultConstraintName", "DF_tbl_ContractItem_DiscountMode");

            migrationBuilder.DropColumn(
                name: "FixedDiscountAmount",
                table: "tbl_ContractItem")
                .Annotation("Relational:DefaultConstraintName", "DF_tbl_ContractItem_FixedDiscountAmount");

            migrationBuilder.DropColumn(
                name: "IsTaxable",
                table: "tbl_ContractItem")
                .Annotation("Relational:DefaultConstraintName", "DF_tbl_ContractItem_IsTaxable");

            migrationBuilder.DropColumn(
                name: "Subtotal",
                table: "tbl_Contract");

            migrationBuilder.DropColumn(
                name: "TotalDiscount",
                table: "tbl_Contract");

            migrationBuilder.DropColumn(
                name: "TotalVat",
                table: "tbl_Contract");
        }
    }
}
