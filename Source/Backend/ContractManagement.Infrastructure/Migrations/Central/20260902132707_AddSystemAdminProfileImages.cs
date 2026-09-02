using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractManagement.Infrastructure.Migrations.Central
{
    /// <inheritdoc />
    public partial class AddSystemAdminProfileImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AvatarContentType",
                table: "SystemAdmins",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AvatarFileSize",
                table: "SystemAdmins",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AvatarSha256",
                table: "SystemAdmins",
                type: "varchar(64)",
                unicode: false,
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AvatarStorageKey",
                table: "SystemAdmins",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AvatarUpdatedAt",
                table: "SystemAdmins",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CoverContentType",
                table: "SystemAdmins",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CoverFileSize",
                table: "SystemAdmins",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CoverSha256",
                table: "SystemAdmins",
                type: "varchar(64)",
                unicode: false,
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CoverStorageKey",
                table: "SystemAdmins",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CoverUpdatedAt",
                table: "SystemAdmins",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvatarContentType",
                table: "SystemAdmins");

            migrationBuilder.DropColumn(
                name: "AvatarFileSize",
                table: "SystemAdmins");

            migrationBuilder.DropColumn(
                name: "AvatarSha256",
                table: "SystemAdmins");

            migrationBuilder.DropColumn(
                name: "AvatarStorageKey",
                table: "SystemAdmins");

            migrationBuilder.DropColumn(
                name: "AvatarUpdatedAt",
                table: "SystemAdmins");

            migrationBuilder.DropColumn(
                name: "CoverContentType",
                table: "SystemAdmins");

            migrationBuilder.DropColumn(
                name: "CoverFileSize",
                table: "SystemAdmins");

            migrationBuilder.DropColumn(
                name: "CoverSha256",
                table: "SystemAdmins");

            migrationBuilder.DropColumn(
                name: "CoverStorageKey",
                table: "SystemAdmins");

            migrationBuilder.DropColumn(
                name: "CoverUpdatedAt",
                table: "SystemAdmins");
        }
    }
}
