using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractManagement.Infrastructure.Migrations.Application
{
    /// <inheritdoc />
    public partial class Phase8A_AddLegalContactAndBankDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BankAccountNumber",
                table: "tbl_TenantLegalProfile",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankName",
                table: "tbl_TenantLegalProfile",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FaxNumber",
                table: "tbl_TenantLegalProfile",
                type: "varchar(30)",
                unicode: false,
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "tbl_TenantLegalProfile",
                type: "varchar(30)",
                unicode: false,
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerBankAccountNumber",
                table: "tbl_Customers",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerBankName",
                table: "tbl_Customers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BankAccountNumber",
                table: "tbl_TenantLegalProfile");

            migrationBuilder.DropColumn(
                name: "BankName",
                table: "tbl_TenantLegalProfile");

            migrationBuilder.DropColumn(
                name: "FaxNumber",
                table: "tbl_TenantLegalProfile");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "tbl_TenantLegalProfile");

            migrationBuilder.DropColumn(
                name: "CustomerBankAccountNumber",
                table: "tbl_Customers");

            migrationBuilder.DropColumn(
                name: "CustomerBankName",
                table: "tbl_Customers");
        }
    }
}
