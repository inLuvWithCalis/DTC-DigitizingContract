using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractManagement.Infrastructure.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddEmployeeAccountSecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "MustChangePassword",
                table: "tbl_Employee",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordChangedAt",
                table: "tbl_Employee",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SessionVersion",
                table: "tbl_Employee",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "ChangedFields",
                table: "tbl_AuthorizationAudit",
                type: "varchar(1000)",
                unicode: false,
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MustChangePassword",
                table: "tbl_Employee");

            migrationBuilder.DropColumn(
                name: "PasswordChangedAt",
                table: "tbl_Employee");

            migrationBuilder.DropColumn(
                name: "SessionVersion",
                table: "tbl_Employee");

            migrationBuilder.DropColumn(
                name: "ChangedFields",
                table: "tbl_AuthorizationAudit");
        }
    }
}
