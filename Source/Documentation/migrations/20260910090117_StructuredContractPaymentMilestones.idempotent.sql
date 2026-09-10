BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910090117_StructuredContractPaymentMilestones'
)
BEGIN
    ALTER TABLE [tbl_ContractTerm] ADD [TermKind] tinyint NOT NULL CONSTRAINT [DF_tbl_ContractTerm_TermKind] DEFAULT CAST(0 AS tinyint);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910090117_StructuredContractPaymentMilestones'
)
BEGIN
    ALTER TABLE [tbl_ContractTemplateTerm] ADD [TermKind] tinyint NOT NULL CONSTRAINT [DF_tbl_ContractTemplateTerm_TermKind] DEFAULT CAST(0 AS tinyint);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910090117_StructuredContractPaymentMilestones'
)
BEGIN
    ALTER TABLE [tbl_ContractPaymentLedger] ADD [PaymentMilestoneId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910090117_StructuredContractPaymentMilestones'
)
BEGIN
    CREATE TABLE [tbl_ContractPaymentMilestone] (
        [PaymentMilestoneId] int NOT NULL IDENTITY,
        [ContractId] int NOT NULL,
        [VersionId] int NOT NULL,
        [TermId] int NOT NULL,
        [SourceTemplatePaymentMilestoneId] int NULL,
        [MilestoneCode] varchar(100) NOT NULL,
        [TitleVi] nvarchar(500) NOT NULL,
        [TitleEn] nvarchar(500) NULL,
        [PaymentPercent] decimal(9,4) NOT NULL,
        [DueAnchor] tinyint NOT NULL,
        [DueOffsetDays] int NOT NULL,
        [DayCountMode] tinyint NOT NULL,
        [ConditionVi] nvarchar(2000) NULL,
        [ConditionEn] nvarchar(2000) NULL,
        [DisplayOrder] int NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [AnchorDate] date NULL,
        [DueDate] date NULL,
        [CreatedEmployeeId] int NOT NULL,
        [CreatedDate] datetime2 NOT NULL DEFAULT ((sysutcdatetime())),
        [UpdatedEmployeeId] int NULL,
        [UpdatedDate] datetime2 NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_tbl_ContractPaymentMilestone] PRIMARY KEY ([PaymentMilestoneId]),
        CONSTRAINT [CK_tbl_ContractPaymentMilestone_Amount] CHECK ([Amount] >= 0),
        CONSTRAINT [CK_tbl_ContractPaymentMilestone_DayCount] CHECK ([DayCountMode] IN (1, 2)),
        CONSTRAINT [CK_tbl_ContractPaymentMilestone_DueAnchor] CHECK ([DueAnchor] IN (1, 2, 3, 4, 5)),
        CONSTRAINT [CK_tbl_ContractPaymentMilestone_DueOffset] CHECK ([DueOffsetDays] >= 0),
        CONSTRAINT [CK_tbl_ContractPaymentMilestone_Percent] CHECK ([PaymentPercent] > 0 AND [PaymentPercent] <= 100),
        CONSTRAINT [FK_tbl_ContractPaymentMilestone_tbl_ContractTerm_TermId] FOREIGN KEY ([TermId]) REFERENCES [tbl_ContractTerm] ([TermId]) ON DELETE CASCADE,
        CONSTRAINT [FK_tbl_ContractPaymentMilestone_tbl_ContractVersion_VersionId] FOREIGN KEY ([VersionId]) REFERENCES [tbl_ContractVersion] ([VersionId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_tbl_ContractPaymentMilestone_tbl_Contract_ContractId] FOREIGN KEY ([ContractId]) REFERENCES [tbl_Contract] ([ContractId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910090117_StructuredContractPaymentMilestones'
)
BEGIN
    CREATE TABLE [tbl_ContractTemplatePaymentMilestone] (
        [TemplatePaymentMilestoneId] int NOT NULL IDENTITY,
        [TemplateVersionId] int NOT NULL,
        [TemplateTermId] int NOT NULL,
        [MilestoneCode] varchar(100) NOT NULL,
        [TitleVi] nvarchar(500) NOT NULL,
        [TitleEn] nvarchar(500) NULL,
        [PaymentPercent] decimal(9,4) NOT NULL,
        [DueAnchor] tinyint NOT NULL,
        [DueOffsetDays] int NOT NULL,
        [DayCountMode] tinyint NOT NULL,
        [ConditionVi] nvarchar(2000) NULL,
        [ConditionEn] nvarchar(2000) NULL,
        [DisplayOrder] int NOT NULL,
        [CreatedEmployeeId] int NOT NULL,
        [CreatedDate] datetime2 NOT NULL DEFAULT ((sysutcdatetime())),
        [UpdatedEmployeeId] int NULL,
        [UpdatedDate] datetime2 NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_tbl_ContractTemplatePaymentMilestone] PRIMARY KEY ([TemplatePaymentMilestoneId]),
        CONSTRAINT [CK_tbl_ContractTemplatePaymentMilestone_DayCount] CHECK ([DayCountMode] IN (1, 2)),
        CONSTRAINT [CK_tbl_ContractTemplatePaymentMilestone_DueAnchor] CHECK ([DueAnchor] IN (1, 2, 3, 4, 5)),
        CONSTRAINT [CK_tbl_ContractTemplatePaymentMilestone_DueOffset] CHECK ([DueOffsetDays] >= 0),
        CONSTRAINT [CK_tbl_ContractTemplatePaymentMilestone_Percent] CHECK ([PaymentPercent] > 0 AND [PaymentPercent] <= 100),
        CONSTRAINT [FK_tbl_ContractTemplatePaymentMilestone_tbl_ContractTemplateTerm_TemplateTermId] FOREIGN KEY ([TemplateTermId]) REFERENCES [tbl_ContractTemplateTerm] ([TemplateTermId]) ON DELETE CASCADE,
        CONSTRAINT [FK_tbl_ContractTemplatePaymentMilestone_tbl_ContractTemplateVersion_TemplateVersionId] FOREIGN KEY ([TemplateVersionId]) REFERENCES [tbl_ContractTemplateVersion] ([TemplateVersionId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910090117_StructuredContractPaymentMilestones'
)
BEGIN
    EXEC(N'ALTER TABLE [tbl_ContractTerm] ADD CONSTRAINT [CK_tbl_ContractTerm_TermKind] CHECK ([TermKind] IN (0, 1))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910090117_StructuredContractPaymentMilestones'
)
BEGIN
    EXEC(N'ALTER TABLE [tbl_ContractTemplateTerm] ADD CONSTRAINT [CK_tbl_ContractTemplateTerm_TermKind] CHECK ([TermKind] IN (0, 1))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910090117_StructuredContractPaymentMilestones'
)
BEGIN
    CREATE INDEX [IX_tbl_ContractPaymentLedger_PaymentMilestoneId] ON [tbl_ContractPaymentLedger] ([PaymentMilestoneId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910090117_StructuredContractPaymentMilestones'
)
BEGIN
    CREATE INDEX [IX_tbl_ContractPaymentMilestone_ContractId_VersionId_DisplayOrder] ON [tbl_ContractPaymentMilestone] ([ContractId], [VersionId], [DisplayOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910090117_StructuredContractPaymentMilestones'
)
BEGIN
    CREATE INDEX [IX_tbl_ContractPaymentMilestone_TermId] ON [tbl_ContractPaymentMilestone] ([TermId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910090117_StructuredContractPaymentMilestones'
)
BEGIN
    CREATE UNIQUE INDEX [IX_tbl_ContractPaymentMilestone_VersionId_MilestoneCode] ON [tbl_ContractPaymentMilestone] ([VersionId], [MilestoneCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910090117_StructuredContractPaymentMilestones'
)
BEGIN
    CREATE UNIQUE INDEX [IX_tbl_ContractTemplatePaymentMilestone_TemplateTermId_DisplayOrder] ON [tbl_ContractTemplatePaymentMilestone] ([TemplateTermId], [DisplayOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910090117_StructuredContractPaymentMilestones'
)
BEGIN
    CREATE UNIQUE INDEX [IX_tbl_ContractTemplatePaymentMilestone_TemplateVersionId_MilestoneCode] ON [tbl_ContractTemplatePaymentMilestone] ([TemplateVersionId], [MilestoneCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910090117_StructuredContractPaymentMilestones'
)
BEGIN
    ALTER TABLE [tbl_ContractPaymentLedger] ADD CONSTRAINT [FK_tbl_ContractPaymentLedger_tbl_ContractPaymentMilestone_PaymentMilestoneId] FOREIGN KEY ([PaymentMilestoneId]) REFERENCES [tbl_ContractPaymentMilestone] ([PaymentMilestoneId]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910090117_StructuredContractPaymentMilestones'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260910090117_StructuredContractPaymentMilestones', N'10.0.0');
END;

COMMIT;
GO

