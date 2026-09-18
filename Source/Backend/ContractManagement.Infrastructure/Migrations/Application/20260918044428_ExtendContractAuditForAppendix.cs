using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractManagement.Infrastructure.Migrations.Application
{
    /// <inheritdoc />
    public partial class ExtendContractAuditForAppendix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractAuditValue_FieldCode",
                table: "tbl_ContractAuditValue");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractAuditValue_FieldKind",
                table: "tbl_ContractAuditValue");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractAuditValue_FieldCode",
                table: "tbl_ContractAuditValue",
                sql: "[FieldCode] BETWEEN 1 AND 95");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractAuditValue_FieldKind",
                table: "tbl_ContractAuditValue",
                sql: "([ValueKind] = 2 AND [FieldCode] IN (11,12,13,14,55,60,61)) OR ([ValueKind] = 4 AND [FieldCode] IN (8,9,45,54,62,63,64,85,91,92,95)) OR ([ValueKind] = 5 AND [FieldCode] IN (27,68)) OR ([ValueKind] = 3 AND [FieldCode] IN (5,6,7,10,17,18,19,20,21,22,32,34,36,43,47,48,56,57,73,74,77,79,80,82,87,90,93)) OR ([ValueKind] = 1 AND [FieldCode] NOT IN (5,6,7,8,9,10,11,12,13,14,17,18,19,20,21,22,27,32,34,36,43,45,47,48,54,55,56,57,60,61,62,63,64,68,73,74,77,79,80,82,85,87,90,91,92,93,95))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractAuditValue_FieldCode",
                table: "tbl_ContractAuditValue");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractAuditValue_FieldKind",
                table: "tbl_ContractAuditValue");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractAuditValue_FieldCode",
                table: "tbl_ContractAuditValue",
                sql: "[FieldCode] BETWEEN 1 AND 93");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractAuditValue_FieldKind",
                table: "tbl_ContractAuditValue",
                sql: "([ValueKind] = 2 AND [FieldCode] IN (11,12,13,14,55,60,61)) OR ([ValueKind] = 4 AND [FieldCode] IN (8,9,45,54,62,63,64,85,91,92)) OR ([ValueKind] = 5 AND [FieldCode] IN (27,68)) OR ([ValueKind] = 3 AND [FieldCode] IN (5,6,7,10,17,18,19,20,21,22,32,34,36,43,47,48,56,57,73,74,77,79,80,82,87,90,93)) OR ([ValueKind] = 1 AND [FieldCode] NOT IN (5,6,7,8,9,10,11,12,13,14,17,18,19,20,21,22,27,32,34,36,43,45,47,48,54,55,56,57,60,61,62,63,64,68,73,74,77,79,80,82,85,87,90,91,92,93))");
        }
    }
}
