using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractManagement.Infrastructure.Migrations.Application
{
    /// <inheritdoc />
    public partial class NormalizeAuditStoragePhase123 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DISABLE TRIGGER [TR_tbl_ContractAudit_AppendOnly]\n    ON [tbl_ContractAudit];\n\nDELETE FROM [tbl_ContractPlaceholderAudit];\nDELETE FROM [tbl_ContractTemplateAudit];\nDELETE FROM [tbl_ContractAudit];\n\nENABLE TRIGGER [TR_tbl_ContractAudit_AppendOnly]\n    ON [tbl_ContractAudit];");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractTemplateAudit_NewValuesJson",
                table: "tbl_ContractTemplateAudit");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractTemplateAudit_PreviousValuesJson",
                table: "tbl_ContractTemplateAudit");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractAudit_NewValuesJson",
                table: "tbl_ContractAudit");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractAudit_PreviousValuesJson",
                table: "tbl_ContractAudit");

            migrationBuilder.DropColumn(
                name: "NewValuesJson",
                table: "tbl_ContractTemplateAudit");

            migrationBuilder.DropColumn(
                name: "PreviousValuesJson",
                table: "tbl_ContractTemplateAudit");

            migrationBuilder.DropColumn(
                name: "NewValuesJson",
                table: "tbl_ContractPlaceholderAudit");

            migrationBuilder.DropColumn(
                name: "PreviousValuesJson",
                table: "tbl_ContractPlaceholderAudit");

            migrationBuilder.DropColumn(
                name: "NewValuesJson",
                table: "tbl_ContractAudit");

            migrationBuilder.DropColumn(
                name: "PreviousValuesJson",
                table: "tbl_ContractAudit");

            migrationBuilder.AddColumn<string>(
                name: "NewFormatString",
                table: "tbl_ContractPlaceholderAudit",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "NewIsActive",
                table: "tbl_ContractPlaceholderAudit",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "NewSourceFieldKey",
                table: "tbl_ContractPlaceholderAudit",
                type: "varchar(200)",
                unicode: false,
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PreviousFormatString",
                table: "tbl_ContractPlaceholderAudit",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PreviousIsActive",
                table: "tbl_ContractPlaceholderAudit",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreviousSourceFieldKey",
                table: "tbl_ContractPlaceholderAudit",
                type: "varchar(200)",
                unicode: false,
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "tbl_ContractAuditValue",
                columns: table => new
                {
                    ContractAuditValueId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractAuditId = table.Column<int>(type: "int", nullable: false),
                    ValueSide = table.Column<byte>(type: "tinyint", nullable: false),
                    FieldCode = table.Column<short>(type: "smallint", nullable: false),
                    ValueKind = table.Column<byte>(type: "tinyint", nullable: false),
                    IsNull = table.Column<bool>(type: "bit", nullable: false),
                    IntegerValue = table.Column<long>(type: "bigint", nullable: true),
                    DecimalValue = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    StringValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DateTimeValue = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BooleanValue = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_ContractAuditValue", x => x.ContractAuditValueId);
                    table.CheckConstraint("CK_tbl_ContractAuditValue_FieldCode", "[FieldCode] BETWEEN 1 AND 93");
                    table.CheckConstraint("CK_tbl_ContractAuditValue_FieldKind", "([ValueKind] = 2 AND [FieldCode] IN (11,12,13,14,55,60,61)) OR ([ValueKind] = 4 AND [FieldCode] IN (8,9,45,54,62,63,64,85,91,92)) OR ([ValueKind] = 5 AND [FieldCode] IN (27,68)) OR ([ValueKind] = 3 AND [FieldCode] IN (5,6,7,10,17,18,19,20,21,22,32,34,36,43,47,48,56,57,73,74,77,79,80,82,87,90,93)) OR ([ValueKind] = 1 AND [FieldCode] NOT IN (5,6,7,8,9,10,11,12,13,14,17,18,19,20,21,22,27,32,34,36,43,45,47,48,54,55,56,57,60,61,62,63,64,68,73,74,77,79,80,82,85,87,90,91,92,93))");
                    table.CheckConstraint("CK_tbl_ContractAuditValue_Side", "[ValueSide] IN (1, 2)");
                    table.CheckConstraint("CK_tbl_ContractAuditValue_Value", "([IsNull] = 1 AND [IntegerValue] IS NULL AND [DecimalValue] IS NULL AND [StringValue] IS NULL AND [DateTimeValue] IS NULL AND [BooleanValue] IS NULL) OR ([IsNull] = 0 AND (([ValueKind] = 1 AND [IntegerValue] IS NOT NULL AND [DecimalValue] IS NULL AND [StringValue] IS NULL AND [DateTimeValue] IS NULL AND [BooleanValue] IS NULL) OR ([ValueKind] = 2 AND [IntegerValue] IS NULL AND [DecimalValue] IS NOT NULL AND [StringValue] IS NULL AND [DateTimeValue] IS NULL AND [BooleanValue] IS NULL) OR ([ValueKind] = 3 AND [IntegerValue] IS NULL AND [DecimalValue] IS NULL AND [StringValue] IS NOT NULL AND [DateTimeValue] IS NULL AND [BooleanValue] IS NULL) OR ([ValueKind] = 4 AND [IntegerValue] IS NULL AND [DecimalValue] IS NULL AND [StringValue] IS NULL AND [DateTimeValue] IS NOT NULL AND [BooleanValue] IS NULL) OR ([ValueKind] = 5 AND [IntegerValue] IS NULL AND [DecimalValue] IS NULL AND [StringValue] IS NULL AND [DateTimeValue] IS NULL AND [BooleanValue] IS NOT NULL)))");
                    table.CheckConstraint("CK_tbl_ContractAuditValue_ValueKind", "[ValueKind] IN (1, 2, 3, 4, 5)");
                    table.ForeignKey(
                        name: "FK_tbl_ContractAuditValue_tbl_ContractAudit_ContractAuditId",
                        column: x => x.ContractAuditId,
                        principalTable: "tbl_ContractAudit",
                        principalColumn: "ContractAuditId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbl_ContractTemplateAuditValue",
                columns: table => new
                {
                    ContractTemplateAuditValueId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractTemplateAuditId = table.Column<int>(type: "int", nullable: false),
                    ValueSide = table.Column<byte>(type: "tinyint", nullable: false),
                    FieldCode = table.Column<byte>(type: "tinyint", nullable: false),
                    IsNull = table.Column<bool>(type: "bit", nullable: false),
                    IntegerValue = table.Column<int>(type: "int", nullable: true),
                    LongValue = table.Column<long>(type: "bigint", nullable: true),
                    StringValue = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_ContractTemplateAuditValue", x => x.ContractTemplateAuditValueId);
                    table.CheckConstraint("CK_tbl_ContractTemplateAuditValue_FieldCode", "[FieldCode] BETWEEN 1 AND 11");
                    table.CheckConstraint("CK_tbl_ContractTemplateAuditValue_NullField", "[IsNull] = 0 OR [FieldCode] IN (1, 6, 9)");
                    table.CheckConstraint("CK_tbl_ContractTemplateAuditValue_Side", "[ValueSide] IN (1, 2)");
                    table.CheckConstraint("CK_tbl_ContractTemplateAuditValue_Status", "([FieldCode] <> 2 OR [StringValue] IN ('doc','docx','docm','dotx','dotm','other')) AND ([FieldCode] <> 4 OR [StringValue] IN ('Valid','Invalid','Unchanged')) AND ([FieldCode] <> 8 OR [StringValue] IN ('Current','Rejected','Stale','Unchanged')) AND ([FieldCode] <> 11 OR [StringValue] IN ('Draft','Published','Retired','Unchanged'))");
                    table.CheckConstraint("CK_tbl_ContractTemplateAuditValue_Value", "([IsNull] = 1 AND [IntegerValue] IS NULL AND [LongValue] IS NULL AND [StringValue] IS NULL) OR ([IsNull] = 0 AND (([FieldCode] IN (1, 5, 6, 9) AND [IntegerValue] IS NOT NULL AND [LongValue] IS NULL AND [StringValue] IS NULL) OR ([FieldCode] IN (3, 7, 10) AND [IntegerValue] IS NULL AND [LongValue] IS NOT NULL AND [StringValue] IS NULL) OR ([FieldCode] IN (2, 4, 8, 11) AND [IntegerValue] IS NULL AND [LongValue] IS NULL AND [StringValue] IS NOT NULL)))");
                    table.ForeignKey(
                        name: "FK_tbl_ContractTemplateAuditValue_tbl_ContractTemplateAudit_ContractTemplateAuditId",
                        column: x => x.ContractTemplateAuditId,
                        principalTable: "tbl_ContractTemplateAudit",
                        principalColumn: "ContractTemplateAuditId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractPlaceholderAudit_ActionType",
                table: "tbl_ContractPlaceholderAudit",
                sql: "[ActionType] IN ('PlaceholderDefinitionCreated', 'PlaceholderDefinitionUpdated', 'PlaceholderDefinitionActivated', 'PlaceholderDefinitionDeactivated', 'PlaceholderDefinitionDeleted')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractPlaceholderAudit_ActorEmployeeId",
                table: "tbl_ContractPlaceholderAudit",
                sql: "[ActorEmployeeId] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractPlaceholderAudit_NewSourceFieldKey",
                table: "tbl_ContractPlaceholderAudit",
                sql: "LEN(LTRIM(RTRIM([NewSourceFieldKey]))) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractPlaceholderAudit_Snapshot",
                table: "tbl_ContractPlaceholderAudit",
                sql: "([ActionType] = 'PlaceholderDefinitionCreated' AND [PreviousSourceFieldKey] IS NULL AND [PreviousFormatString] IS NULL AND [PreviousIsActive] IS NULL) OR ([ActionType] <> 'PlaceholderDefinitionCreated' AND LEN(LTRIM(RTRIM([PreviousSourceFieldKey]))) > 0)");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractAuditValue_Audit_Side",
                table: "tbl_ContractAuditValue",
                columns: new[] { "ContractAuditId", "ValueSide" });

            migrationBuilder.CreateIndex(
                name: "UX_tbl_ContractAuditValue_Audit_Side_Field",
                table: "tbl_ContractAuditValue",
                columns: new[] { "ContractAuditId", "ValueSide", "FieldCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractTemplateAuditValue_Audit_Side",
                table: "tbl_ContractTemplateAuditValue",
                columns: new[] { "ContractTemplateAuditId", "ValueSide" });

            migrationBuilder.CreateIndex(
                name: "UX_tbl_ContractTemplateAuditValue_Audit_Side_Field",
                table: "tbl_ContractTemplateAuditValue",
                columns: new[] { "ContractTemplateAuditId", "ValueSide", "FieldCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbl_ContractAuditValue");

            migrationBuilder.DropTable(
                name: "tbl_ContractTemplateAuditValue");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractPlaceholderAudit_ActionType",
                table: "tbl_ContractPlaceholderAudit");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractPlaceholderAudit_ActorEmployeeId",
                table: "tbl_ContractPlaceholderAudit");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractPlaceholderAudit_NewSourceFieldKey",
                table: "tbl_ContractPlaceholderAudit");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractPlaceholderAudit_Snapshot",
                table: "tbl_ContractPlaceholderAudit");

            migrationBuilder.DropColumn(
                name: "NewFormatString",
                table: "tbl_ContractPlaceholderAudit");

            migrationBuilder.DropColumn(
                name: "NewIsActive",
                table: "tbl_ContractPlaceholderAudit");

            migrationBuilder.DropColumn(
                name: "NewSourceFieldKey",
                table: "tbl_ContractPlaceholderAudit");

            migrationBuilder.DropColumn(
                name: "PreviousFormatString",
                table: "tbl_ContractPlaceholderAudit");

            migrationBuilder.DropColumn(
                name: "PreviousIsActive",
                table: "tbl_ContractPlaceholderAudit");

            migrationBuilder.DropColumn(
                name: "PreviousSourceFieldKey",
                table: "tbl_ContractPlaceholderAudit");

            migrationBuilder.AddColumn<string>(
                name: "NewValuesJson",
                table: "tbl_ContractTemplateAudit",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreviousValuesJson",
                table: "tbl_ContractTemplateAudit",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NewValuesJson",
                table: "tbl_ContractPlaceholderAudit",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PreviousValuesJson",
                table: "tbl_ContractPlaceholderAudit",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NewValuesJson",
                table: "tbl_ContractAudit",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreviousValuesJson",
                table: "tbl_ContractAudit",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractTemplateAudit_NewValuesJson",
                table: "tbl_ContractTemplateAudit",
                sql: "[NewValuesJson] IS NULL OR (ISJSON([NewValuesJson]) = 1 AND LEFT(LTRIM([NewValuesJson]), 1) = '{')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractTemplateAudit_PreviousValuesJson",
                table: "tbl_ContractTemplateAudit",
                sql: "[PreviousValuesJson] IS NULL OR (ISJSON([PreviousValuesJson]) = 1 AND LEFT(LTRIM([PreviousValuesJson]), 1) = '{')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractAudit_NewValuesJson",
                table: "tbl_ContractAudit",
                sql: "[NewValuesJson] IS NULL OR (ISJSON([NewValuesJson]) = 1 AND LEFT(LTRIM([NewValuesJson]), 1) = '{')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractAudit_PreviousValuesJson",
                table: "tbl_ContractAudit",
                sql: "[PreviousValuesJson] IS NULL OR (ISJSON([PreviousValuesJson]) = 1 AND LEFT(LTRIM([PreviousValuesJson]), 1) = '{')");
        }
    }
}
