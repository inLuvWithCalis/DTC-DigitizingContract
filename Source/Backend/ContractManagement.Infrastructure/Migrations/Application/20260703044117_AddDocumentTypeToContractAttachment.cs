using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractManagement.Infrastructure.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddDocumentTypeToContractAttachment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "DocumentType",
                table: "tbl_ContractAttachment",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)99);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DocumentType",
                table: "tbl_ContractAttachment");
        }
    }
}
