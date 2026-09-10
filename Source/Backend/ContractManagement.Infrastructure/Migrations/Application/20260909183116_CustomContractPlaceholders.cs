using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractManagement.Infrastructure.Migrations.Application
{
    /// <inheritdoc />
    public partial class CustomContractPlaceholders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PlaceholderBindingHash",
                table: "tbl_ContractTemplateVersion",
                type: "varchar(64)",
                unicode: false,
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "DataKind",
                table: "tbl_ContractTemplateField",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<byte[]>(
                name: "DefinitionRowVersion",
                table: "tbl_ContractTemplateField",
                type: "varbinary(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSystem",
                table: "tbl_ContractTemplateField",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte>(
                name: "Multiplicity",
                table: "tbl_ContractTemplateField",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<string>(
                name: "SourceFieldKey",
                table: "tbl_ContractTemplateField",
                type: "varchar(200)",
                unicode: false,
                maxLength: 200,
                nullable: true);

            // Frozen V2 compatibility map. Legacy rendering is keyed by PlaceholderKey;
            // DataSource was descriptive metadata and changed between catalog revisions.
            // Accept a known system key and canonicalize its metadata, but fail closed for
            // an unknown key because it cannot be rendered safely after this migration.
            migrationBuilder.Sql("""
                DECLARE @Legacy TABLE ([Key] varchar(100), [Source] varchar(500), [Kind] tinyint, [Multiplicity] tinyint);
                INSERT INTO @Legacy VALUES
                ('CONTRACT_CODE', 'Contract.ContractCode', 1, 1),
                ('CONTRACT_NAME', 'Contract.ContractName', 1, 1),
                ('CONTRACT_DATE', 'Contract.CreatedDate', 1, 1),
                ('EFFECTIVE_DATE', 'Contract.EffectiveDate', 1, 1),
                ('EXPIRE_DATE', 'Contract.ExpireDate', 1, 1),
                ('CONTRACT_CURRENCY', 'Contract.CurrencyCode', 1, 1),
                ('CUSTOMER_CODE', 'Customer.CustomerCode', 1, 1),
                ('CUSTOMER_NAME', 'Customer.CustomerFullName', 1, 1),
                ('CUSTOMER_REPRESENTATIVE_TITLE', 'Customer.CustomerRepresentativeTitle', 1, 1),
                ('CUSTOMER_COMPANY', 'Customer.CustomerCompany', 1, 1),
                ('CUSTOMER_TAX_CODE', 'Customer.CustomerTaxCode', 1, 1),
                ('CUSTOMER_ADDRESS', 'Customer.CustomerAddress', 1, 1),
                ('CUSTOMER_EMAIL', 'Customer.CustomerEmail', 1, 1),
                ('CUSTOMER_PHONE', 'Customer.CustomerPhone', 1, 1),
                ('PROVIDER_LEGAL_NAME', 'TenantLegalProfile.LegalEntityName', 1, 1),
                ('PROVIDER_TAX_CODE', 'TenantLegalProfile.TaxCode', 1, 1),
                ('PROVIDER_ADDRESS', 'TenantLegalProfile.Address', 1, 1),
                ('PROVIDER_REPRESENTATIVE_NAME', 'TenantLegalProfile.RepresentativeName', 1, 1),
                ('PROVIDER_REPRESENTATIVE_TITLE', 'TenantLegalProfile.RepresentativeTitle', 1, 1),
                ('CONTRACT_TERMS', 'Contract.Terms', 2, 1),
                ('CONTRACT_ITEM_TABLE', 'Contract.Items', 2, 1),
                ('SIGNATURE_PROVIDER', 'Contract.ProviderSignature', 2, 1),
                ('SIGNATURE_CUSTOMER', 'Contract.CustomerSignature', 2, 1),
                ('CONTRACT_NAME_EN', 'Contract.ContractNameEn', 1, 2),
                ('CUSTOMER_FAX', 'Customer.CustomerFaxNumber', 1, 2),
                ('CUSTOMER_BANK_ACCOUNT_NUMBER', 'Customer.CustomerBankAccountNumber', 1, 2),
                ('CUSTOMER_BANK_NAME', 'Customer.CustomerBankName', 1, 2),
                ('PROVIDER_PHONE', 'TenantLegalProfile.PhoneNumber', 1, 2),
                ('PROVIDER_FAX', 'TenantLegalProfile.FaxNumber', 1, 2),
                ('PROVIDER_BANK_ACCOUNT_NUMBER', 'TenantLegalProfile.BankAccountNumber', 1, 2),
                ('PROVIDER_BANK_NAME', 'TenantLegalProfile.BankName', 1, 2),
                ('CUSTOMER_WEBSITE', 'Customer.CustomerWebsite', 1, 2),
                ('CUSTOMER_CITY', 'Customer.CustomerCity', 1, 2),
                ('CUSTOMER_COUNTRY', 'Customer.CustomerCountry', 1, 2),
                ('CONTRACT_TOTAL_AMOUNT', 'Contract.TotalAmount', 1, 2),
                ('CONTRACT_TOTAL_AMOUNT_IN_WORDS', 'Manual.ContractTotalAmountInWords', 1, 2),
                ('PAYMENT_SCHEDULE_TABLE', 'Contract.PaymentSchedules', 2, 2);
                DECLARE @UnknownKey varchar(100), @UnknownSource varchar(500), @UnknownMessage nvarchar(2048);
                SELECT TOP (1) @UnknownKey = f.PlaceholderKey, @UnknownSource = f.DataSource
                FROM tbl_ContractTemplateField f
                WHERE NOT EXISTS (SELECT 1 FROM @Legacy l WHERE l.[Key] = f.PlaceholderKey)
                ORDER BY f.PlaceholderKey, f.DataSource;
                IF @UnknownKey IS NOT NULL
                BEGIN
                    SET @UnknownMessage = CONCAT(
                        'Unknown legacy placeholder key: ', @UnknownKey,
                        ' (DataSource: ', COALESCE(@UnknownSource, '<null>'), '). Review before migrating.');
                    THROW 51000, @UnknownMessage, 1;
                END;
                UPDATE f SET PlaceholderKey = l.[Key], DataSource = l.[Source],
                    SourceFieldKey = 'system.' + l.[Key], IsSystem = 1,
                    DataKind = l.[Kind], Multiplicity = l.[Multiplicity]
                FROM tbl_ContractTemplateField f JOIN @Legacy l ON l.[Key] = f.PlaceholderKey;
                """);

            migrationBuilder.CreateTable(
                name: "tbl_ContractPlaceholderAudit",
                columns: table => new
                {
                    PlaceholderAuditId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlaceholderKey = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    ActorEmployeeId = table.Column<int>(type: "int", nullable: false),
                    ActionType = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    PreviousValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_ContractPlaceholderAudit", x => x.PlaceholderAuditId);
                });

            migrationBuilder.CreateTable(
                name: "tbl_ContractPlaceholderDefinition",
                columns: table => new
                {
                    PlaceholderDefinitionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlaceholderKey = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    FieldLabel = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    SourceFieldKey = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    DefaultValue = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    FormatString = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedEmployeeId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedEmployeeId = table.Column<int>(type: "int", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_ContractPlaceholderDefinition", x => x.PlaceholderDefinitionId);
                });

            migrationBuilder.CreateTable(
                name: "tbl_ContractVersionPlaceholderValue",
                columns: table => new
                {
                    ContractVersionPlaceholderValueId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractId = table.Column<int>(type: "int", nullable: false),
                    VersionId = table.Column<int>(type: "int", nullable: false),
                    TemplateVersionId = table.Column<int>(type: "int", nullable: false),
                    PlaceholderKey = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    SourceFieldKey = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    RawValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RenderedValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CapturedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_ContractVersionPlaceholderValue", x => x.ContractVersionPlaceholderValueId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractPlaceholderAudit_PlaceholderKey_OccurredAt",
                table: "tbl_ContractPlaceholderAudit",
                columns: new[] { "PlaceholderKey", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractPlaceholderDefinition_PlaceholderKey",
                table: "tbl_ContractPlaceholderDefinition",
                column: "PlaceholderKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractVersionPlaceholderValue_ContractId_VersionId",
                table: "tbl_ContractVersionPlaceholderValue",
                columns: new[] { "ContractId", "VersionId" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractVersionPlaceholderValue_VersionId_PlaceholderKey",
                table: "tbl_ContractVersionPlaceholderValue",
                columns: new[] { "VersionId", "PlaceholderKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbl_ContractPlaceholderAudit");

            migrationBuilder.DropTable(
                name: "tbl_ContractPlaceholderDefinition");

            migrationBuilder.DropTable(
                name: "tbl_ContractVersionPlaceholderValue");

            migrationBuilder.DropColumn(
                name: "PlaceholderBindingHash",
                table: "tbl_ContractTemplateVersion");

            migrationBuilder.DropColumn(
                name: "DataKind",
                table: "tbl_ContractTemplateField");

            migrationBuilder.DropColumn(
                name: "DefinitionRowVersion",
                table: "tbl_ContractTemplateField");

            migrationBuilder.DropColumn(
                name: "IsSystem",
                table: "tbl_ContractTemplateField");

            migrationBuilder.DropColumn(
                name: "Multiplicity",
                table: "tbl_ContractTemplateField");

            migrationBuilder.DropColumn(
                name: "SourceFieldKey",
                table: "tbl_ContractTemplateField");
        }
    }
}
