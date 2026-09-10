BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909183116_CustomContractPlaceholders'
)
BEGIN
    ALTER TABLE [tbl_ContractTemplateVersion] ADD [PlaceholderBindingHash] varchar(64) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909183116_CustomContractPlaceholders'
)
BEGIN
    ALTER TABLE [tbl_ContractTemplateField] ADD [DataKind] tinyint NOT NULL DEFAULT CAST(0 AS tinyint);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909183116_CustomContractPlaceholders'
)
BEGIN
    ALTER TABLE [tbl_ContractTemplateField] ADD [DefinitionRowVersion] varbinary(8) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909183116_CustomContractPlaceholders'
)
BEGIN
    ALTER TABLE [tbl_ContractTemplateField] ADD [IsSystem] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909183116_CustomContractPlaceholders'
)
BEGIN
    ALTER TABLE [tbl_ContractTemplateField] ADD [Multiplicity] tinyint NOT NULL DEFAULT CAST(0 AS tinyint);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909183116_CustomContractPlaceholders'
)
BEGIN
    ALTER TABLE [tbl_ContractTemplateField] ADD [SourceFieldKey] varchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909183116_CustomContractPlaceholders'
)
BEGIN
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
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909183116_CustomContractPlaceholders'
)
BEGIN
    CREATE TABLE [tbl_ContractPlaceholderAudit] (
        [PlaceholderAuditId] bigint NOT NULL IDENTITY,
        [PlaceholderKey] varchar(100) NOT NULL,
        [ActorEmployeeId] int NOT NULL,
        [ActionType] varchar(64) NOT NULL,
        [PreviousValuesJson] nvarchar(max) NULL,
        [NewValuesJson] nvarchar(max) NOT NULL,
        [OccurredAt] datetime2 NOT NULL,
        CONSTRAINT [PK_tbl_ContractPlaceholderAudit] PRIMARY KEY ([PlaceholderAuditId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909183116_CustomContractPlaceholders'
)
BEGIN
    CREATE TABLE [tbl_ContractPlaceholderDefinition] (
        [PlaceholderDefinitionId] int NOT NULL IDENTITY,
        [PlaceholderKey] varchar(100) NOT NULL,
        [FieldLabel] nvarchar(300) NOT NULL,
        [SourceFieldKey] varchar(200) NOT NULL,
        [DefaultValue] nvarchar(2000) NULL,
        [FormatString] varchar(100) NULL,
        [IsActive] bit NOT NULL,
        [CreatedEmployeeId] int NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedEmployeeId] int NULL,
        [UpdatedDate] datetime2 NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_tbl_ContractPlaceholderDefinition] PRIMARY KEY ([PlaceholderDefinitionId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909183116_CustomContractPlaceholders'
)
BEGIN
    CREATE TABLE [tbl_ContractVersionPlaceholderValue] (
        [ContractVersionPlaceholderValueId] bigint NOT NULL IDENTITY,
        [ContractId] int NOT NULL,
        [VersionId] int NOT NULL,
        [TemplateVersionId] int NOT NULL,
        [PlaceholderKey] varchar(100) NOT NULL,
        [SourceFieldKey] varchar(200) NOT NULL,
        [RawValue] nvarchar(max) NULL,
        [RenderedValue] nvarchar(max) NOT NULL,
        [CapturedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_tbl_ContractVersionPlaceholderValue] PRIMARY KEY ([ContractVersionPlaceholderValueId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909183116_CustomContractPlaceholders'
)
BEGIN
    CREATE INDEX [IX_tbl_ContractPlaceholderAudit_PlaceholderKey_OccurredAt] ON [tbl_ContractPlaceholderAudit] ([PlaceholderKey], [OccurredAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909183116_CustomContractPlaceholders'
)
BEGIN
    CREATE UNIQUE INDEX [IX_tbl_ContractPlaceholderDefinition_PlaceholderKey] ON [tbl_ContractPlaceholderDefinition] ([PlaceholderKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909183116_CustomContractPlaceholders'
)
BEGIN
    CREATE INDEX [IX_tbl_ContractVersionPlaceholderValue_ContractId_VersionId] ON [tbl_ContractVersionPlaceholderValue] ([ContractId], [VersionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909183116_CustomContractPlaceholders'
)
BEGIN
    CREATE UNIQUE INDEX [IX_tbl_ContractVersionPlaceholderValue_VersionId_PlaceholderKey] ON [tbl_ContractVersionPlaceholderValue] ([VersionId], [PlaceholderKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909183116_CustomContractPlaceholders'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260909183116_CustomContractPlaceholders', N'10.0.0');
END;

COMMIT;
GO

