using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractManagement.Infrastructure.Migrations.Application
{
    /// <inheritdoc />
    public partial class Phase8A_PrivateFileMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "tbl_FileStorage",
                type: "varchar(255)",
                unicode: false,
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sha256",
                table: "tbl_FileStorage",
                type: "char(64)",
                unicode: false,
                fixedLength: true,
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StorageKey",
                table: "tbl_FileStorage",
                type: "varchar(1000)",
                unicode: false,
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenantCode",
                table: "tbl_FileStorage",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "tbl_FileStorage");

            migrationBuilder.DropColumn(
                name: "Sha256",
                table: "tbl_FileStorage");

            migrationBuilder.DropColumn(
                name: "StorageKey",
                table: "tbl_FileStorage");

            migrationBuilder.DropColumn(
                name: "TenantCode",
                table: "tbl_FileStorage");
        }
    }
}
