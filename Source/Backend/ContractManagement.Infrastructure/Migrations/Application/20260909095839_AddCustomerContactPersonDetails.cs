using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractManagement.Infrastructure.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddCustomerContactPersonDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomerContactPersonName",
                table: "tbl_Customers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerContactPersonPhone",
                table: "tbl_Customers",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerContactPersonTitle",
                table: "tbl_Customers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerContactPersonName",
                table: "tbl_Customers");

            migrationBuilder.DropColumn(
                name: "CustomerContactPersonPhone",
                table: "tbl_Customers");

            migrationBuilder.DropColumn(
                name: "CustomerContactPersonTitle",
                table: "tbl_Customers");
        }
    }
}
