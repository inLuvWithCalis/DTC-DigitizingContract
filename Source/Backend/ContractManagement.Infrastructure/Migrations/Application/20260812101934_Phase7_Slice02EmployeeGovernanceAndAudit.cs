using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractManagement.Infrastructure.Migrations.Application
{
    /// <inheritdoc />
    public partial class Phase7_Slice02EmployeeGovernanceAndAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "tbl_Employee",
                type: "rowversion",
                rowVersion: true,
                nullable: false);

            migrationBuilder.CreateTable(
                name: "tbl_AuthorizationAudit",
                columns: table => new
                {
                    AuthorizationAuditId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    ActorEmployeeId = table.Column<int>(type: "int", nullable: true),
                    ActorType = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    Action = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Result = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    FailureCode = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: true),
                    TargetType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    TargetId = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    PreviousEmployeeType = table.Column<byte>(type: "tinyint", nullable: true),
                    NewEmployeeType = table.Column<byte>(type: "tinyint", nullable: true),
                    PreviousStatus = table.Column<byte>(type: "tinyint", nullable: true),
                    NewStatus = table.Column<byte>(type: "tinyint", nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IpAddress = table.Column<string>(type: "varchar(45)", unicode: false, maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    CorrelationId = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_AuthorizationAudit", x => x.AuthorizationAuditId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_AuthorizationAudit_ActorEmployeeId",
                table: "tbl_AuthorizationAudit",
                column: "ActorEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_AuthorizationAudit_TenantId_OccurredAt",
                table: "tbl_AuthorizationAudit",
                columns: new[] { "TenantId", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbl_AuthorizationAudit");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "tbl_Employee");
        }
    }
}
