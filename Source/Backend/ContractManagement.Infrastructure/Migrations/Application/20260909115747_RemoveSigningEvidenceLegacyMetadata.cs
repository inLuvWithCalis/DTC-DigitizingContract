using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractManagement.Infrastructure.Migrations.Application
{
    /// <inheritdoc />
    public partial class RemoveSigningEvidenceLegacyMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerSignerName",
                table: "tbl_ContractSignedEvidence");

            migrationBuilder.DropColumn(
                name: "CustomerSignerTitle",
                table: "tbl_ContractSignedEvidence");

            migrationBuilder.DropColumn(
                name: "CustomerSigningDate",
                table: "tbl_ContractSignedEvidence");

            migrationBuilder.DropColumn(
                name: "ProviderSignerName",
                table: "tbl_ContractSignedEvidence");

            migrationBuilder.DropColumn(
                name: "ProviderSignerTitle",
                table: "tbl_ContractSignedEvidence");

            migrationBuilder.DropColumn(
                name: "ProviderSigningDate",
                table: "tbl_ContractSignedEvidence");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomerSignerName",
                table: "tbl_ContractSignedEvidence",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CustomerSignerTitle",
                table: "tbl_ContractSignedEvidence",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CustomerSigningDate",
                table: "tbl_ContractSignedEvidence",
                type: "date",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "ProviderSignerName",
                table: "tbl_ContractSignedEvidence",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProviderSignerTitle",
                table: "tbl_ContractSignedEvidence",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ProviderSigningDate",
                table: "tbl_ContractSignedEvidence",
                type: "date",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
