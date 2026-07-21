using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractManagement.Infrastructure.Migrations.Application
{
    /// <inheritdoc />
    public partial class Phase6_ContractVersionFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
             /*
             * Migration bổ sung CreatedEmployeeId bắt buộc nhưng không tự tạo ID giả.
             * Vì chưa có dữ liệu thật, migration chỉ được chạy khi bảng đang rỗng.
             */
            migrationBuilder.Sql(
                        """
            IF EXISTS (SELECT 1 FROM [dbo].[tbl_ContractVersion])
            BEGIN
                ;THROW 51001,
                    N'Phase6_ContractVersionFoundation requires tbl_ContractVersion to be empty.',
                    1;
            END;
            """);

            migrationBuilder.DropPrimaryKey(
                name: "PK__tbl_Cont__16C6400F510A7D28",
                table: "tbl_ContractVersion");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedDate",
                table: "tbl_ContractVersion",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "(sysutcdatetime())",
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldDefaultValueSql: "(getdate())")
                .Annotation("Relational:DefaultConstraintName", "DF_tbl_ContractVersion_CreatedDate");

            migrationBuilder.AddColumn<int>(
                name: "CreatedEmployeeId",
                table: "tbl_ContractVersion",
                type: "int",
                nullable: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsLocked",
                table: "tbl_ContractVersion",
                type: "bit",
                nullable: false,
                defaultValue: false)
                .Annotation("Relational:DefaultConstraintName", "DF_tbl_ContractVersion_IsLocked");

            migrationBuilder.AddColumn<int>(
                name: "LockedByEmployeeId",
                table: "tbl_ContractVersion",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockedDate",
                table: "tbl_ContractVersion",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "tbl_ContractVersion",
                type: "rowversion",
                rowVersion: true,
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "SnapshotHash",
                table: "tbl_ContractVersion",
                type: "char(64)",
                unicode: false,
                fixedLength: true,
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SnapshotJson",
                table: "tbl_ContractVersion",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceVersionId",
                table: "tbl_ContractVersion",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TemplateVersionId",
                table: "tbl_ContractVersion",
                type: "int",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_tbl_ContractVersion",
                table: "tbl_ContractVersion",
                column: "VersionId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractVersion_SourceVersionId",
                table: "tbl_ContractVersion",
                column: "SourceVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractVersion_TemplateVersionId",
                table: "tbl_ContractVersion",
                column: "TemplateVersionId");

            migrationBuilder.CreateIndex(
                name: "UX_tbl_ContractVersion_ContractId_VersionNo",
                table: "tbl_ContractVersion",
                columns: new[] { "ContractId", "VersionNo" },
                unique: true);

            /*
             * Các cột IsLocked, LockedDate, LockedByEmployeeId,
             * SnapshotJson và SnapshotHash vừa được thêm trong migration này.
             * Dynamic SQL trì hoãn việc biên dịch constraint cho tới khi các cột tồn tại.
             */
            migrationBuilder.Sql(
                "EXEC(N'ALTER TABLE [dbo].[tbl_ContractVersion] " +
                "ADD CONSTRAINT [CK_tbl_ContractVersion_LockState] " +
                "CHECK (([IsLocked] = 0 " +
                "AND [LockedDate] IS NULL " +
                "AND [LockedByEmployeeId] IS NULL) " +
                "OR ([IsLocked] = 1 " +
                "AND [LockedDate] IS NOT NULL " +
                "AND [LockedByEmployeeId] IS NOT NULL " +
                "AND [SnapshotJson] IS NOT NULL " +
                "AND [SnapshotHash] IS NOT NULL));');");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractVersion_VersionNo",
                table: "tbl_ContractVersion",
                sql: "[VersionNo] > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_tbl_ContractVersion",
                table: "tbl_ContractVersion");

            migrationBuilder.DropIndex(
                name: "IX_tbl_ContractVersion_SourceVersionId",
                table: "tbl_ContractVersion");

            migrationBuilder.DropIndex(
                name: "IX_tbl_ContractVersion_TemplateVersionId",
                table: "tbl_ContractVersion");

            migrationBuilder.DropIndex(
                name: "UX_tbl_ContractVersion_ContractId_VersionNo",
                table: "tbl_ContractVersion");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractVersion_LockState",
                table: "tbl_ContractVersion");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractVersion_VersionNo",
                table: "tbl_ContractVersion");

            migrationBuilder.DropColumn(
                name: "CreatedEmployeeId",
                table: "tbl_ContractVersion");

            migrationBuilder.DropColumn(
                name: "IsLocked",
                table: "tbl_ContractVersion")
                .Annotation("Relational:DefaultConstraintName", "DF_tbl_ContractVersion_IsLocked");

            migrationBuilder.DropColumn(
                name: "LockedByEmployeeId",
                table: "tbl_ContractVersion");

            migrationBuilder.DropColumn(
                name: "LockedDate",
                table: "tbl_ContractVersion");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "tbl_ContractVersion");

            migrationBuilder.DropColumn(
                name: "SnapshotHash",
                table: "tbl_ContractVersion");

            migrationBuilder.DropColumn(
                name: "SnapshotJson",
                table: "tbl_ContractVersion");

            migrationBuilder.DropColumn(
                name: "SourceVersionId",
                table: "tbl_ContractVersion");

            migrationBuilder.DropColumn(
                name: "TemplateVersionId",
                table: "tbl_ContractVersion");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedDate",
                table: "tbl_ContractVersion",
                type: "datetime",
                nullable: false,
                defaultValueSql: "(getdate())",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "(sysutcdatetime())")
                .OldAnnotation("Relational:DefaultConstraintName", "DF_tbl_ContractVersion_CreatedDate");

            migrationBuilder.AddPrimaryKey(
                name: "PK__tbl_Cont__16C6400F510A7D28",
                table: "tbl_ContractVersion",
                column: "VersionId");
        }
    }
}
