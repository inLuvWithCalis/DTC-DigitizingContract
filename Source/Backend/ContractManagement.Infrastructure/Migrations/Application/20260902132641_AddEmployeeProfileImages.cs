using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractManagement.Infrastructure.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddEmployeeProfileImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AvatarContentType",
                table: "tbl_Employee",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AvatarFileSize",
                table: "tbl_Employee",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AvatarSha256",
                table: "tbl_Employee",
                type: "varchar(64)",
                unicode: false,
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AvatarStorageKey",
                table: "tbl_Employee",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AvatarUpdatedAt",
                table: "tbl_Employee",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CoverContentType",
                table: "tbl_Employee",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CoverFileSize",
                table: "tbl_Employee",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CoverSha256",
                table: "tbl_Employee",
                type: "varchar(64)",
                unicode: false,
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CoverStorageKey",
                table: "tbl_Employee",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CoverUpdatedAt",
                table: "tbl_Employee",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvatarContentType",
                table: "tbl_Employee");

            migrationBuilder.DropColumn(
                name: "AvatarFileSize",
                table: "tbl_Employee");

            migrationBuilder.DropColumn(
                name: "AvatarSha256",
                table: "tbl_Employee");

            migrationBuilder.DropColumn(
                name: "AvatarStorageKey",
                table: "tbl_Employee");

            migrationBuilder.DropColumn(
                name: "AvatarUpdatedAt",
                table: "tbl_Employee");

            migrationBuilder.DropColumn(
                name: "CoverContentType",
                table: "tbl_Employee");

            migrationBuilder.DropColumn(
                name: "CoverFileSize",
                table: "tbl_Employee");

            migrationBuilder.DropColumn(
                name: "CoverSha256",
                table: "tbl_Employee");

            migrationBuilder.DropColumn(
                name: "CoverStorageKey",
                table: "tbl_Employee");

            migrationBuilder.DropColumn(
                name: "CoverUpdatedAt",
                table: "tbl_Employee");
        }
    }
}
