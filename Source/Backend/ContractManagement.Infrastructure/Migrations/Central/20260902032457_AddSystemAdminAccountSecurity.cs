using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractManagement.Infrastructure.Migrations.Central
{
    /// <inheritdoc />
    public partial class AddSystemAdminAccountSecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "MustChangePassword",
                table: "SystemAdmins",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordChangedAt",
                table: "SystemAdmins",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "SystemAdmins",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "SessionVersion",
                table: "SystemAdmins",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "SystemAdmins",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChangedFields",
                table: "CentralSecurityAudits",
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
                table: "SystemAdmins");

            migrationBuilder.DropColumn(
                name: "PasswordChangedAt",
                table: "SystemAdmins");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "SystemAdmins");

            migrationBuilder.DropColumn(
                name: "SessionVersion",
                table: "SystemAdmins");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "SystemAdmins");

            migrationBuilder.DropColumn(
                name: "ChangedFields",
                table: "CentralSecurityAudits");
        }
    }
}
