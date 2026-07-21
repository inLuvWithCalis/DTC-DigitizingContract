using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractManagement.Infrastructure.Migrations.Application
{
    /// <inheritdoc />
    public partial class Phase6_ContractCoreFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
            """
            IF EXISTS (SELECT 1 FROM [dbo].[tbl_Contract])
            BEGIN
                ;THROW 51000,
                    N'Phase6_ContractCoreFoundation requires tbl_Contract to be empty.',
                    1;
            END;
            """);
            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdateDate",
                table: "tbl_Contract",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalAmount",
                table: "tbl_Contract",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true);

            migrationBuilder.AlterColumn<byte>(
                name: "Status",
                table: "tbl_Contract",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0,
                oldClrType: typeof(byte),
                oldType: "tinyint",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "SignDate",
                table: "tbl_Contract",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ExpireDate",
                table: "tbl_Contract",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "EmployeeId",
                table: "tbl_Contract",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "EffectiveDate",
                table: "tbl_Contract",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CustomerId",
                table: "tbl_Contract",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CreatedEmployeeId",
                table: "tbl_Contract",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedDate",
                table: "tbl_Contract",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "(sysutcdatetime())",
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true,
                oldDefaultValueSql: "(getdate())")
                .Annotation("Relational:DefaultConstraintName", "DF_tbl_Contract_CreatedDate")
                .OldAnnotation("Relational:DefaultConstraintName", "DF_tbl_Contract_CreatedDate");

            migrationBuilder.AlterColumn<string>(
                name: "ContractNameEn",
                table: "tbl_Contract",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(500)",
                oldUnicode: false,
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ContractName",
                table: "tbl_Contract",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "ContractType",
                table: "tbl_Contract",
                type: "tinyint",
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "CurrencyCode",
                table: "tbl_Contract",
                type: "char(3)",
                unicode: false,
                fixedLength: true,
                maxLength: 3,
                nullable: false,
                defaultValue: "VND");

            migrationBuilder.AddColumn<int>(
                name: "CurrentVersionId",
                table: "tbl_Contract",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsLegacy",
                table: "tbl_Contract",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte>(
                name: "LanguageMode",
                table: "tbl_Contract",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)1);

            migrationBuilder.AddColumn<int>(
                name: "ParentContractId",
                table: "tbl_Contract",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "tbl_Contract",
                type: "rowversion",
                rowVersion: true,
                nullable: false);

            migrationBuilder.AddColumn<int>(
                name: "TemplateVersionId",
                table: "tbl_Contract",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_Contract_CurrentVersionId",
                table: "tbl_Contract",
                column: "CurrentVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_Contract_CustomerId",
                table: "tbl_Contract",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_Contract_EmployeeId_Status",
                table: "tbl_Contract",
                columns: new[] { "EmployeeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_Contract_ParentContractId",
                table: "tbl_Contract",
                column: "ParentContractId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_Contract_TemplateVersionId",
                table: "tbl_Contract",
                column: "TemplateVersionId");

            migrationBuilder.CreateIndex(
                name: "UX_tbl_Contract_ContractCode",
                table: "tbl_Contract",
                column: "ContractCode",
                unique: true,
                filter: "[ContractCode] IS NOT NULL");

            /*
             * ContractType và LanguageMode là hai cột vừa được thêm trong migration này.
             * Dùng dynamic SQL để SQL Server chỉ biên dịch constraint
             * sau khi các cột đã thực sự tồn tại.
             */
            migrationBuilder.Sql(
                "EXEC(N'ALTER TABLE [dbo].[tbl_Contract] " +
                "ADD CONSTRAINT [CK_tbl_Contract_ContractType] " +
                "CHECK ([ContractType] IN (1, 2, 3));');");

            migrationBuilder.Sql(
                "EXEC(N'ALTER TABLE [dbo].[tbl_Contract] " +
                "ADD CONSTRAINT [CK_tbl_Contract_LanguageMode] " +
                "CHECK ([LanguageMode] IN (1, 2));');");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_Contract_Status",
                table: "tbl_Contract",
                sql: "[Status] IN (0, 1, 2, 3, 4, 5, 6, 7)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_Contract_TotalAmount",
                table: "tbl_Contract",
                sql: "[TotalAmount] >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tbl_Contract_CurrentVersionId",
                table: "tbl_Contract");

            migrationBuilder.DropIndex(
                name: "IX_tbl_Contract_CustomerId",
                table: "tbl_Contract");

            migrationBuilder.DropIndex(
                name: "IX_tbl_Contract_EmployeeId_Status",
                table: "tbl_Contract");

            migrationBuilder.DropIndex(
                name: "IX_tbl_Contract_ParentContractId",
                table: "tbl_Contract");

            migrationBuilder.DropIndex(
                name: "IX_tbl_Contract_TemplateVersionId",
                table: "tbl_Contract");

            migrationBuilder.DropIndex(
                name: "UX_tbl_Contract_ContractCode",
                table: "tbl_Contract");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_Contract_ContractType",
                table: "tbl_Contract");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_Contract_LanguageMode",
                table: "tbl_Contract");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_Contract_Status",
                table: "tbl_Contract");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_Contract_TotalAmount",
                table: "tbl_Contract");

            migrationBuilder.DropColumn(
                name: "ContractType",
                table: "tbl_Contract");

            migrationBuilder.DropColumn(
                name: "CurrencyCode",
                table: "tbl_Contract");

            migrationBuilder.DropColumn(
                name: "CurrentVersionId",
                table: "tbl_Contract");

            migrationBuilder.DropColumn(
                name: "IsLegacy",
                table: "tbl_Contract");

            migrationBuilder.DropColumn(
                name: "LanguageMode",
                table: "tbl_Contract");

            migrationBuilder.DropColumn(
                name: "ParentContractId",
                table: "tbl_Contract");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "tbl_Contract");

            migrationBuilder.DropColumn(
                name: "TemplateVersionId",
                table: "tbl_Contract");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdateDate",
                table: "tbl_Contract",
                type: "datetime",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "TotalAmount",
                table: "tbl_Contract",
                type: "float",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<byte>(
                name: "Status",
                table: "tbl_Contract",
                type: "tinyint",
                nullable: true,
                oldClrType: typeof(byte),
                oldType: "tinyint",
                oldDefaultValue: (byte)0);

            migrationBuilder.AlterColumn<DateTime>(
                name: "SignDate",
                table: "tbl_Contract",
                type: "datetime",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ExpireDate",
                table: "tbl_Contract",
                type: "datetime",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "EmployeeId",
                table: "tbl_Contract",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<DateTime>(
                name: "EffectiveDate",
                table: "tbl_Contract",
                type: "datetime",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CustomerId",
                table: "tbl_Contract",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "CreatedEmployeeId",
                table: "tbl_Contract",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedDate",
                table: "tbl_Contract",
                type: "datetime",
                nullable: true,
                defaultValueSql: "(getdate())",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "(sysutcdatetime())")
                .Annotation("Relational:DefaultConstraintName", "DF_tbl_Contract_CreatedDate")
                .OldAnnotation("Relational:DefaultConstraintName", "DF_tbl_Contract_CreatedDate");

            migrationBuilder.AlterColumn<string>(
                name: "ContractNameEn",
                table: "tbl_Contract",
                type: "varchar(500)",
                unicode: false,
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ContractName",
                table: "tbl_Contract",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);
        }
    }
}
