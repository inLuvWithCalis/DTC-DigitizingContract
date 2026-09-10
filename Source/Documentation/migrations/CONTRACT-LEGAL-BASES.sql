BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910045945_AddContractLegalBases'
)
BEGIN
    CREATE TABLE [tbl_ContractLegalBasis] (
        [LegalBasisId] int NOT NULL IDENTITY,
        [ContractId] int NOT NULL,
        [VersionId] int NOT NULL,
        [SourceTemplateLegalBasisId] int NULL,
        [BasisCode] varchar(100) NOT NULL,
        [ContentVi] nvarchar(max) NOT NULL,
        [ContentEn] nvarchar(max) NULL,
        [DisplayOrder] int NOT NULL CONSTRAINT [DF_tbl_ContractLegalBasis_DisplayOrder] DEFAULT 0,
        [CreatedEmployeeId] int NOT NULL,
        [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_tbl_ContractLegalBasis_CreatedDate] DEFAULT ((sysutcdatetime())),
        [UpdatedEmployeeId] int NULL,
        [UpdatedDate] datetime2 NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_tbl_ContractLegalBasis] PRIMARY KEY ([LegalBasisId]),
        CONSTRAINT [CK_tbl_ContractLegalBasis_BasisCode] CHECK (LEN(LTRIM(RTRIM([BasisCode]))) > 0),
        CONSTRAINT [CK_tbl_ContractLegalBasis_ContentVi] CHECK (LEN(LTRIM(RTRIM([ContentVi]))) > 0),
        CONSTRAINT [CK_tbl_ContractLegalBasis_ContractId] CHECK ([ContractId] > 0),
        CONSTRAINT [CK_tbl_ContractLegalBasis_DisplayOrder] CHECK ([DisplayOrder] >= 0),
        CONSTRAINT [CK_tbl_ContractLegalBasis_SourceTemplateLegalBasisId] CHECK ([SourceTemplateLegalBasisId] IS NULL OR [SourceTemplateLegalBasisId] > 0),
        CONSTRAINT [CK_tbl_ContractLegalBasis_VersionId] CHECK ([VersionId] > 0)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910045945_AddContractLegalBases'
)
BEGIN
    CREATE TABLE [tbl_ContractTemplateLegalBasis] (
        [TemplateLegalBasisId] int NOT NULL IDENTITY,
        [TemplateVersionId] int NOT NULL,
        [BasisCode] varchar(100) NOT NULL,
        [ContentVi] nvarchar(max) NOT NULL,
        [ContentEn] nvarchar(max) NULL,
        [DisplayOrder] int NOT NULL CONSTRAINT [DF_tbl_ContractTemplateLegalBasis_DisplayOrder] DEFAULT 0,
        [CreatedEmployeeId] int NOT NULL,
        [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_tbl_ContractTemplateLegalBasis_CreatedDate] DEFAULT ((sysutcdatetime())),
        [UpdatedEmployeeId] int NULL,
        [UpdatedDate] datetime2 NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_tbl_ContractTemplateLegalBasis] PRIMARY KEY ([TemplateLegalBasisId]),
        CONSTRAINT [CK_tbl_ContractTemplateLegalBasis_BasisCode] CHECK (LEN(LTRIM(RTRIM([BasisCode]))) > 0),
        CONSTRAINT [CK_tbl_ContractTemplateLegalBasis_ContentVi] CHECK (LEN(LTRIM(RTRIM([ContentVi]))) > 0),
        CONSTRAINT [CK_tbl_ContractTemplateLegalBasis_DisplayOrder] CHECK ([DisplayOrder] >= 0),
        CONSTRAINT [CK_tbl_ContractTemplateLegalBasis_TemplateVersionId] CHECK ([TemplateVersionId] > 0)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910045945_AddContractLegalBases'
)
BEGIN
    CREATE INDEX [IX_tbl_ContractLegalBasis_Contract_Version_DisplayOrder] ON [tbl_ContractLegalBasis] ([ContractId], [VersionId], [DisplayOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910045945_AddContractLegalBases'
)
BEGIN
    CREATE INDEX [IX_tbl_ContractLegalBasis_SourceTemplateLegalBasisId] ON [tbl_ContractLegalBasis] ([SourceTemplateLegalBasisId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910045945_AddContractLegalBases'
)
BEGIN
    CREATE UNIQUE INDEX [UX_tbl_ContractLegalBasis_Version_BasisCode] ON [tbl_ContractLegalBasis] ([VersionId], [BasisCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910045945_AddContractLegalBases'
)
BEGIN
    CREATE INDEX [IX_tbl_ContractTemplateLegalBasis_Version_DisplayOrder] ON [tbl_ContractTemplateLegalBasis] ([TemplateVersionId], [DisplayOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910045945_AddContractLegalBases'
)
BEGIN
    CREATE UNIQUE INDEX [UX_tbl_ContractTemplateLegalBasis_Version_BasisCode] ON [tbl_ContractTemplateLegalBasis] ([TemplateVersionId], [BasisCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910045945_AddContractLegalBases'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260910045945_AddContractLegalBases', N'10.0.0');
END;

COMMIT;
GO

