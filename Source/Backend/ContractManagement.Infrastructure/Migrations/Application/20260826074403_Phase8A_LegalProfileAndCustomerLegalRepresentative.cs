using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractManagement.Infrastructure.Migrations.Application
{
    /// <inheritdoc />
    public partial class Phase8A_LegalProfileAndCustomerLegalRepresentative : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomerRepresentativeName",
                table: "tbl_Customers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerRepresentativeTitle",
                table: "tbl_Customers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "tbl_TenantLegalProfile",
                columns: table => new
                {
                    TenantLegalProfileId = table.Column<int>(type: "int", nullable: false),
                    LegalEntityName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TaxCode = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    RepresentativeName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RepresentativeTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedByEmployeeId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedByEmployeeId = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_TenantLegalProfile", x => x.TenantLegalProfileId);
                    table.CheckConstraint("CK_tbl_TenantLegalProfile_Employees", "[CreatedByEmployeeId] > 0 AND [UpdatedByEmployeeId] > 0");
                    table.CheckConstraint("CK_tbl_TenantLegalProfile_Singleton", "[TenantLegalProfileId] = 1");
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbl_TenantLegalProfile");

            migrationBuilder.DropColumn(
                name: "CustomerRepresentativeName",
                table: "tbl_Customers");

            migrationBuilder.DropColumn(
                name: "CustomerRepresentativeTitle",
                table: "tbl_Customers");
        }
    }
}
