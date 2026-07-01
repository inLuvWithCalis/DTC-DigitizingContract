using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractManagement.Infrastructure.Migrations.Central
{
    /// <inheritdoc />
    public partial class InitialCentral : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TenantDatabases",
                columns: table => new
                {
                    TenantDatabaseId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DatabaseKey = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    DatabaseName = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: false),
                    ConnectionString = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Mode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantDatabases", x => x.TenantDatabaseId);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    TenantId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantCode = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    TenantName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TenantDatabaseId = table.Column<int>(type: "int", nullable: false),
                    ProvisioningError = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.TenantId);
                    table.ForeignKey(
                        name: "FK_Tenants_TenantDatabases_TenantDatabaseId",
                        column: x => x.TenantDatabaseId,
                        principalTable: "TenantDatabases",
                        principalColumn: "TenantDatabaseId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantDatabases_DatabaseKey",
                table: "TenantDatabases",
                column: "DatabaseKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantDatabases_DatabaseName",
                table: "TenantDatabases",
                column: "DatabaseName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_TenantCode",
                table: "Tenants",
                column: "TenantCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_TenantDatabaseId",
                table: "Tenants",
                column: "TenantDatabaseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropTable(
                name: "TenantDatabases");
        }
    }
}
