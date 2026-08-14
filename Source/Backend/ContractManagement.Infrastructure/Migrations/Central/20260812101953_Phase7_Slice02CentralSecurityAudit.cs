using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractManagement.Infrastructure.Migrations.Central
{
    /// <inheritdoc />
    public partial class Phase7_Slice02CentralSecurityAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CentralSecurityAudits",
                columns: table => new
                {
                    CentralSecurityAuditId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActorSystemAdminId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    TenantCode = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    Action = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Result = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    FailureCode = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: true),
                    TargetType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
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
                    table.PrimaryKey("PK_CentralSecurityAudits", x => x.CentralSecurityAuditId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CentralSecurityAudits_ActorSystemAdminId",
                table: "CentralSecurityAudits",
                column: "ActorSystemAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_CentralSecurityAudits_OccurredAt",
                table: "CentralSecurityAudits",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_CentralSecurityAudits_TenantId_OccurredAt",
                table: "CentralSecurityAudits",
                columns: new[] { "TenantId", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CentralSecurityAudits");
        }
    }
}
