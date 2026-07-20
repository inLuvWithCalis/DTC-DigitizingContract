using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractManagement.Infrastructure.Migrations.Application
{
    /// <inheritdoc />
    public partial class Phase6_ContractTermVersioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            /*
             * VersionId, TermCode và CreatedEmployeeId trở thành dữ liệu bắt buộc.
             * Không tự backfill bằng 0 hoặc chuỗi rỗng.
             */
            migrationBuilder.Sql(
                """
                IF EXISTS (SELECT 1 FROM [dbo].[tbl_ContractTerm])
                BEGIN
                    ;THROW 51002,
                        N'Phase6_ContractTermVersioning requires tbl_ContractTerm to be empty.',
                        1;
                END;
                """);
            migrationBuilder.DropPrimaryKey(
                name: "PK__tbl_Cont__410A21A507DB2563",
                table: "tbl_ContractTerm");

            migrationBuilder.AlterColumn<int>(
                name: "DisplayOrder",
                table: "tbl_ContractTerm",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true,
                oldDefaultValue: 0)
                .Annotation("Relational:DefaultConstraintName", "DF_tbl_ContractTerm_DisplayOrder");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "tbl_ContractTerm",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "(sysutcdatetime())")
                .Annotation("Relational:DefaultConstraintName", "DF_tbl_ContractTerm_CreatedDate");

            migrationBuilder.AddColumn<int>(
                name: "CreatedEmployeeId",
                table: "tbl_ContractTerm",
                type: "int",
                nullable: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsNegotiable",
                table: "tbl_ContractTerm",
                type: "bit",
                nullable: false,
                defaultValue: false)
                .Annotation("Relational:DefaultConstraintName", "DF_tbl_ContractTerm_IsNegotiable");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "tbl_ContractTerm",
                type: "rowversion",
                rowVersion: true,
                nullable: false);

            migrationBuilder.AddColumn<int>(
                name: "SourceTemplateTermId",
                table: "tbl_ContractTerm",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TermCode",
                table: "tbl_ContractTerm",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "TermContentEn",
                table: "tbl_ContractTerm",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TermTitleEn",
                table: "tbl_ContractTerm",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "tbl_ContractTerm",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedEmployeeId",
                table: "tbl_ContractTerm",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VersionId",
                table: "tbl_ContractTerm",
                type: "int",
                nullable: false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_tbl_ContractTerm",
                table: "tbl_ContractTerm",
                column: "TermId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractTerm_ContractId_VersionId_DisplayOrder",
                table: "tbl_ContractTerm",
                columns: new[] { "ContractId", "VersionId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractTerm_SourceTemplateTermId",
                table: "tbl_ContractTerm",
                column: "SourceTemplateTermId");

            migrationBuilder.CreateIndex(
                name: "UX_tbl_ContractTerm_VersionId_TermCode",
                table: "tbl_ContractTerm",
                columns: new[] { "VersionId", "TermCode" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractTerm_DisplayOrder",
                table: "tbl_ContractTerm",
                sql: "[DisplayOrder] >= 0");

            /*
             * TermCode là cột vừa được thêm trong migration.
             * Dynamic SQL trì hoãn biên dịch constraint đến khi cột tồn tại.
             */
            migrationBuilder.Sql(
                "EXEC(N'ALTER TABLE [dbo].[tbl_ContractTerm] " +
                "ADD CONSTRAINT [CK_tbl_ContractTerm_TermCode] " +
                "CHECK (LEN(LTRIM(RTRIM([TermCode]))) > 0);');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_tbl_ContractTerm",
                table: "tbl_ContractTerm");

            migrationBuilder.DropIndex(
                name: "IX_tbl_ContractTerm_ContractId_VersionId_DisplayOrder",
                table: "tbl_ContractTerm");

            migrationBuilder.DropIndex(
                name: "IX_tbl_ContractTerm_SourceTemplateTermId",
                table: "tbl_ContractTerm");

            migrationBuilder.DropIndex(
                name: "UX_tbl_ContractTerm_VersionId_TermCode",
                table: "tbl_ContractTerm");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractTerm_DisplayOrder",
                table: "tbl_ContractTerm");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractTerm_TermCode",
                table: "tbl_ContractTerm");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "tbl_ContractTerm")
                .Annotation("Relational:DefaultConstraintName", "DF_tbl_ContractTerm_CreatedDate");

            migrationBuilder.DropColumn(
                name: "CreatedEmployeeId",
                table: "tbl_ContractTerm");

            migrationBuilder.DropColumn(
                name: "IsNegotiable",
                table: "tbl_ContractTerm")
                .Annotation("Relational:DefaultConstraintName", "DF_tbl_ContractTerm_IsNegotiable");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "tbl_ContractTerm");

            migrationBuilder.DropColumn(
                name: "SourceTemplateTermId",
                table: "tbl_ContractTerm");

            migrationBuilder.DropColumn(
                name: "TermCode",
                table: "tbl_ContractTerm");

            migrationBuilder.DropColumn(
                name: "TermContentEn",
                table: "tbl_ContractTerm");

            migrationBuilder.DropColumn(
                name: "TermTitleEn",
                table: "tbl_ContractTerm");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "tbl_ContractTerm");

            migrationBuilder.DropColumn(
                name: "UpdatedEmployeeId",
                table: "tbl_ContractTerm");

            migrationBuilder.DropColumn(
                name: "VersionId",
                table: "tbl_ContractTerm");

            migrationBuilder.AlterColumn<int>(
                name: "DisplayOrder",
                table: "tbl_ContractTerm",
                type: "int",
                nullable: true,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0)
                .OldAnnotation("Relational:DefaultConstraintName", "DF_tbl_ContractTerm_DisplayOrder");

            migrationBuilder.AddPrimaryKey(
                name: "PK__tbl_Cont__410A21A507DB2563",
                table: "tbl_ContractTerm",
                column: "TermId");
        }
    }
}
