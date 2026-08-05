using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractManagement.Infrastructure.Migrations.Application
{
    /// <inheritdoc />
    public partial class Phase6_Slice06CustomerAccessAndPublicComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractNegotiationCommentEvent_EmployeeId",
                table: "tbl_ContractNegotiationCommentEvent");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractNegotiationComment_RecordedByEmployeeId",
                table: "tbl_ContractNegotiationComment");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractNegotiationComment_Source",
                table: "tbl_ContractNegotiationComment");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractAudit_Actor",
                table: "tbl_ContractAudit");

            migrationBuilder.AlterColumn<int>(
                name: "EmployeeId",
                table: "tbl_ContractNegotiationCommentEvent",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "ActorType",
                table: "tbl_ContractNegotiationCommentEvent",
                type: "varchar(32)",
                unicode: false,
                maxLength: 32,
                nullable: false,
                defaultValue: "Employee");

            migrationBuilder.AddColumn<int>(
                name: "CustomerAccessSessionId",
                table: "tbl_ContractNegotiationCommentEvent",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "RecordedByEmployeeId",
                table: "tbl_ContractNegotiationComment",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "CustomerAccessSessionId",
                table: "tbl_ContractNegotiationComment",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ActorCustomerAccessSessionId",
                table: "tbl_ContractAudit",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrentCustomerAccessLinkId",
                table: "tbl_Contract",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrentVerificationPhoneId",
                table: "tbl_Contract",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "tbl_ContractCustomerAccessLink",
                columns: table => new
                {
                    CustomerAccessLinkId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    ContractId = table.Column<int>(type: "int", nullable: false),
                    VersionId = table.Column<int>(type: "int", nullable: false),
                    VerificationPhoneId = table.Column<int>(type: "int", nullable: false),
                    TokenHash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    CreatedByEmployeeId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    ActivatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedByEmployeeId = table.Column<int>(type: "int", nullable: true),
                    RevocationReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_ContractCustomerAccessLink", x => x.CustomerAccessLinkId);
                    table.CheckConstraint("CK_tbl_ContractCustomerAccessLink_Activation", "[ActivatedAt] IS NULL OR ([ActivatedAt] >= [CreatedDate] AND [ActivatedAt] <= [ExpiresAt])");
                    table.CheckConstraint("CK_tbl_ContractCustomerAccessLink_Expiry", "[ExpiresAt] > [CreatedDate]");
                    table.CheckConstraint("CK_tbl_ContractCustomerAccessLink_LogicalIds", "[TenantId] > 0 AND [ContractId] > 0 AND [VersionId] > 0 AND [VerificationPhoneId] > 0 AND [CreatedByEmployeeId] > 0");
                    table.CheckConstraint("CK_tbl_ContractCustomerAccessLink_Revocation", "([RevokedAt] IS NULL AND [RevokedByEmployeeId] IS NULL AND [RevocationReason] IS NULL) OR ([RevokedAt] IS NOT NULL AND [RevokedByEmployeeId] > 0 AND LEN(LTRIM(RTRIM([RevocationReason]))) BETWEEN 1 AND 1000)");
                    table.CheckConstraint("CK_tbl_ContractCustomerAccessLink_TokenHash", "LEN(LTRIM(RTRIM([TokenHash]))) = 64");
                });

            migrationBuilder.CreateTable(
                name: "tbl_ContractCustomerAccessSession",
                columns: table => new
                {
                    CustomerAccessSessionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    LinkId = table.Column<int>(type: "int", nullable: false),
                    ContractId = table.Column<int>(type: "int", nullable: false),
                    VersionId = table.Column<int>(type: "int", nullable: false),
                    VerificationPhoneId = table.Column<int>(type: "int", nullable: false),
                    SessionTokenHash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastActivityAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IdleExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HardExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevocationReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_ContractCustomerAccessSession", x => x.CustomerAccessSessionId);
                    table.CheckConstraint("CK_tbl_ContractCustomerAccessSession_Expiry", "[IssuedAt] <= [LastActivityAt] AND [IdleExpiresAt] >= [LastActivityAt] AND [HardExpiresAt] >= [IdleExpiresAt]");
                    table.CheckConstraint("CK_tbl_ContractCustomerAccessSession_LogicalIds", "[TenantId] > 0 AND [LinkId] > 0 AND [ContractId] > 0 AND [VersionId] > 0 AND [VerificationPhoneId] > 0");
                    table.CheckConstraint("CK_tbl_ContractCustomerAccessSession_Revocation", "[RevokedAt] IS NULL OR LEN(LTRIM(RTRIM([RevocationReason]))) BETWEEN 1 AND 1000");
                    table.CheckConstraint("CK_tbl_ContractCustomerAccessSession_TokenHash", "LEN(LTRIM(RTRIM([SessionTokenHash]))) = 64");
                });

            migrationBuilder.CreateTable(
                name: "tbl_ContractCustomerOtpChallenge",
                columns: table => new
                {
                    CustomerOtpChallengeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicChallengeId = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    LinkId = table.Column<int>(type: "int", nullable: false),
                    VerificationPhoneId = table.Column<int>(type: "int", nullable: false),
                    Purpose = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    OtpHash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FailedAttemptCount = table.Column<int>(type: "int", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LockedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InvalidatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_ContractCustomerOtpChallenge", x => x.CustomerOtpChallengeId);
                    table.CheckConstraint("CK_tbl_ContractCustomerOtpChallenge_Attempts", "[FailedAttemptCount] BETWEEN 0 AND 5");
                    table.CheckConstraint("CK_tbl_ContractCustomerOtpChallenge_Expiry", "[ExpiresAt] > [CreatedDate]");
                    table.CheckConstraint("CK_tbl_ContractCustomerOtpChallenge_Lock", "[LockedAt] IS NULL OR [FailedAttemptCount] = 5");
                    table.CheckConstraint("CK_tbl_ContractCustomerOtpChallenge_LogicalIds", "[LinkId] > 0 AND [VerificationPhoneId] > 0");
                    table.CheckConstraint("CK_tbl_ContractCustomerOtpChallenge_OtpHash", "LEN(LTRIM(RTRIM([OtpHash]))) = 64");
                    table.CheckConstraint("CK_tbl_ContractCustomerOtpChallenge_PublicId", "LEN(LTRIM(RTRIM([PublicChallengeId]))) BETWEEN 32 AND 64");
                    table.CheckConstraint("CK_tbl_ContractCustomerOtpChallenge_Purpose", "[Purpose] = 'CustomerAccess'");
                });

            migrationBuilder.CreateTable(
                name: "tbl_ContractCustomerOtpDeliveryOutbox",
                columns: table => new
                {
                    CustomerOtpDeliveryOutboxId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChallengeId = table.Column<int>(type: "int", nullable: false),
                    EncryptedPayload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    NextAttemptAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LeaseUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LeaseId = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: true),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastFailure = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_ContractCustomerOtpDeliveryOutbox", x => x.CustomerOtpDeliveryOutboxId);
                    table.CheckConstraint("CK_tbl_ContractCustomerOtpDeliveryOutbox_Attempts", "[AttemptCount] >= 0");
                    table.CheckConstraint("CK_tbl_ContractCustomerOtpDeliveryOutbox_Challenge", "[ChallengeId] > 0");
                    table.CheckConstraint("CK_tbl_ContractCustomerOtpDeliveryOutbox_Payload", "LEN(LTRIM(RTRIM([EncryptedPayload]))) > 0");
                    table.CheckConstraint("CK_tbl_ContractCustomerOtpDeliveryOutbox_Status", "[Status] IN ('Pending', 'Leased', 'Sent', 'Failed')");
                });

            migrationBuilder.CreateTable(
                name: "tbl_ContractCustomerVerificationPhone",
                columns: table => new
                {
                    VerificationPhoneId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractId = table.Column<int>(type: "int", nullable: false),
                    PhoneSource = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    PhoneNumberNormalized = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedByEmployeeId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_ContractCustomerVerificationPhone", x => x.VerificationPhoneId);
                    table.CheckConstraint("CK_tbl_ContractCustomerVerificationPhone_ContractId", "[ContractId] > 0");
                    table.CheckConstraint("CK_tbl_ContractCustomerVerificationPhone_CreatedBy", "[CreatedByEmployeeId] > 0");
                    table.CheckConstraint("CK_tbl_ContractCustomerVerificationPhone_Phone", "LEN(LTRIM(RTRIM([PhoneNumberNormalized]))) BETWEEN 3 AND 32");
                    table.CheckConstraint("CK_tbl_ContractCustomerVerificationPhone_Reason", "LEN(LTRIM(RTRIM([Reason]))) BETWEEN 1 AND 1000");
                    table.CheckConstraint("CK_tbl_ContractCustomerVerificationPhone_Source", "[PhoneSource] IN ('CustomerMobile', 'CustomerPhone', 'Manual')");
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractNegotiationCommentEvent_Actor",
                table: "tbl_ContractNegotiationCommentEvent",
                sql: "([ActorType] = 'Employee' AND [EmployeeId] > 0 AND [CustomerAccessSessionId] IS NULL) OR ([ActorType] = 'Customer' AND [EmployeeId] IS NULL AND [CustomerAccessSessionId] > 0) OR ([ActorType] = 'System' AND [EmployeeId] IS NULL AND [CustomerAccessSessionId] IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractNegotiationComment_Actor",
                table: "tbl_ContractNegotiationComment",
                sql: "([Source] = 'ExternalFeedback' AND [RecordedByEmployeeId] > 0 AND [CustomerAccessSessionId] IS NULL) OR ([Source] = 'Customer' AND [RecordedByEmployeeId] IS NULL AND [CustomerAccessSessionId] > 0)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractNegotiationComment_Source",
                table: "tbl_ContractNegotiationComment",
                sql: "[Source] IN ('ExternalFeedback', 'Customer')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractAudit_Actor",
                table: "tbl_ContractAudit",
                sql: "([ActorType] = 'Employee' AND [ActorEmployeeId] > 0 AND [ActorCustomerAccessSessionId] IS NULL) OR ([ActorType] = 'Customer' AND [ActorEmployeeId] IS NULL AND [ActorCustomerAccessSessionId] > 0) OR ([ActorType] = 'System' AND [ActorEmployeeId] IS NULL AND [ActorCustomerAccessSessionId] IS NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_Contract_CurrentCustomerAccessLinkId",
                table: "tbl_Contract",
                column: "CurrentCustomerAccessLinkId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_Contract_CurrentVerificationPhoneId",
                table: "tbl_Contract",
                column: "CurrentVerificationPhoneId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractCustomerAccessLink_ActiveContext",
                table: "tbl_ContractCustomerAccessLink",
                columns: new[] { "ContractId", "VersionId", "VerificationPhoneId" },
                filter: "[RevokedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractCustomerAccessLink_Expiry",
                table: "tbl_ContractCustomerAccessLink",
                columns: new[] { "ExpiresAt", "RevokedAt" });

            migrationBuilder.CreateIndex(
                name: "UX_tbl_ContractCustomerAccessLink_ActiveContract",
                table: "tbl_ContractCustomerAccessLink",
                column: "ContractId",
                unique: true,
                filter: "[RevokedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "UX_tbl_ContractCustomerAccessLink_TokenHash",
                table: "tbl_ContractCustomerAccessLink",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractCustomerAccessSession_ActiveLink",
                table: "tbl_ContractCustomerAccessSession",
                columns: new[] { "LinkId", "RevokedAt", "IdleExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractCustomerAccessSession_ContractVersion",
                table: "tbl_ContractCustomerAccessSession",
                columns: new[] { "ContractId", "VersionId", "RevokedAt" });

            migrationBuilder.CreateIndex(
                name: "UX_tbl_ContractCustomerAccessSession_TokenHash",
                table: "tbl_ContractCustomerAccessSession",
                column: "SessionTokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractCustomerOtpChallenge_ActiveLookup",
                table: "tbl_ContractCustomerOtpChallenge",
                columns: new[] { "LinkId", "ExpiresAt" },
                filter: "[UsedAt] IS NULL AND [LockedAt] IS NULL AND [InvalidatedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractCustomerOtpChallenge_Link_Created",
                table: "tbl_ContractCustomerOtpChallenge",
                columns: new[] { "LinkId", "CreatedDate" });

            migrationBuilder.CreateIndex(
                name: "UX_tbl_ContractCustomerOtpChallenge_PublicChallengeId",
                table: "tbl_ContractCustomerOtpChallenge",
                column: "PublicChallengeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractCustomerOtpDeliveryOutbox_Lease",
                table: "tbl_ContractCustomerOtpDeliveryOutbox",
                columns: new[] { "Status", "NextAttemptAt", "LeaseUntil" });

            migrationBuilder.CreateIndex(
                name: "UX_tbl_ContractCustomerOtpDeliveryOutbox_ChallengeId",
                table: "tbl_ContractCustomerOtpDeliveryOutbox",
                column: "ChallengeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractCustomerVerificationPhone_Contract_Chronological",
                table: "tbl_ContractCustomerVerificationPhone",
                columns: new[] { "ContractId", "CreatedDate", "VerificationPhoneId" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractCustomerVerificationPhone_Contract_Phone",
                table: "tbl_ContractCustomerVerificationPhone",
                columns: new[] { "ContractId", "PhoneNumberNormalized" });

            migrationBuilder.Sql(
                """
                CREATE OR ALTER TRIGGER [TR_tbl_ContractNegotiationComment_AppendOnly]
                ON [dbo].[tbl_ContractNegotiationComment]
                AFTER UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF EXISTS
                    (
                        SELECT 1
                        FROM [deleted] d
                        LEFT JOIN [inserted] i ON i.[CommentId] = d.[CommentId]
                        WHERE i.[CommentId] IS NULL
                           OR i.[ContractId] <> d.[ContractId]
                           OR i.[VersionId] <> d.[VersionId]
                           OR ISNULL(i.[TermId], 0) <> ISNULL(d.[TermId], 0)
                           OR ISNULL(i.[ParentCommentId], 0) <> ISNULL(d.[ParentCommentId], 0)
                           OR i.[Content] <> d.[Content]
                           OR i.[Source] <> d.[Source]
                           OR ISNULL(i.[RecordedByEmployeeId], 0) <> ISNULL(d.[RecordedByEmployeeId], 0)
                           OR ISNULL(i.[CustomerAccessSessionId], 0) <> ISNULL(d.[CustomerAccessSessionId], 0)
                           OR i.[CreatedDate] <> d.[CreatedDate]
                    )
                    BEGIN
                        ROLLBACK TRANSACTION;
                        THROW 51005, N'Negotiation comments are append-only.', 1;
                    END;
                END;
                """);

            migrationBuilder.Sql(
                """
                CREATE OR ALTER TRIGGER [TR_tbl_ContractNegotiationCommentEvent_AppendOnly]
                ON [dbo].[tbl_ContractNegotiationCommentEvent]
                AFTER UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF EXISTS
                    (
                        SELECT 1
                        FROM [deleted] d
                        LEFT JOIN [inserted] i ON i.[CommentEventId] = d.[CommentEventId]
                        WHERE i.[CommentEventId] IS NULL
                           OR i.[CommentId] <> d.[CommentId]
                           OR i.[EventType] <> d.[EventType]
                           OR i.[ActorType] <> d.[ActorType]
                           OR ISNULL(i.[EmployeeId], 0) <> ISNULL(d.[EmployeeId], 0)
                           OR ISNULL(i.[CustomerAccessSessionId], 0) <> ISNULL(d.[CustomerAccessSessionId], 0)
                           OR i.[OccurredAt] <> d.[OccurredAt]
                    )
                    BEGIN
                        ROLLBACK TRANSACTION;
                        THROW 51006, N'Negotiation comment events are append-only.', 1;
                    END;
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbl_ContractCustomerAccessLink");

            migrationBuilder.DropTable(
                name: "tbl_ContractCustomerAccessSession");

            migrationBuilder.DropTable(
                name: "tbl_ContractCustomerOtpChallenge");

            migrationBuilder.DropTable(
                name: "tbl_ContractCustomerOtpDeliveryOutbox");

            migrationBuilder.DropTable(
                name: "tbl_ContractCustomerVerificationPhone");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractNegotiationCommentEvent_Actor",
                table: "tbl_ContractNegotiationCommentEvent");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractNegotiationComment_Actor",
                table: "tbl_ContractNegotiationComment");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractNegotiationComment_Source",
                table: "tbl_ContractNegotiationComment");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractAudit_Actor",
                table: "tbl_ContractAudit");

            migrationBuilder.DropIndex(
                name: "IX_tbl_Contract_CurrentCustomerAccessLinkId",
                table: "tbl_Contract");

            migrationBuilder.DropIndex(
                name: "IX_tbl_Contract_CurrentVerificationPhoneId",
                table: "tbl_Contract");

            migrationBuilder.DropColumn(
                name: "ActorType",
                table: "tbl_ContractNegotiationCommentEvent");

            migrationBuilder.DropColumn(
                name: "CustomerAccessSessionId",
                table: "tbl_ContractNegotiationCommentEvent");

            migrationBuilder.DropColumn(
                name: "CustomerAccessSessionId",
                table: "tbl_ContractNegotiationComment");

            migrationBuilder.DropColumn(
                name: "ActorCustomerAccessSessionId",
                table: "tbl_ContractAudit");

            migrationBuilder.DropColumn(
                name: "CurrentCustomerAccessLinkId",
                table: "tbl_Contract");

            migrationBuilder.DropColumn(
                name: "CurrentVerificationPhoneId",
                table: "tbl_Contract");

            migrationBuilder.AlterColumn<int>(
                name: "EmployeeId",
                table: "tbl_ContractNegotiationCommentEvent",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "RecordedByEmployeeId",
                table: "tbl_ContractNegotiationComment",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractNegotiationCommentEvent_EmployeeId",
                table: "tbl_ContractNegotiationCommentEvent",
                sql: "[EmployeeId] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractNegotiationComment_RecordedByEmployeeId",
                table: "tbl_ContractNegotiationComment",
                sql: "[RecordedByEmployeeId] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractNegotiationComment_Source",
                table: "tbl_ContractNegotiationComment",
                sql: "[Source] = 'ExternalFeedback'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractAudit_Actor",
                table: "tbl_ContractAudit",
                sql: "([ActorType] = 'Employee' AND [ActorEmployeeId] > 0) OR ([ActorType] <> 'Employee' AND [ActorEmployeeId] IS NULL)");

            migrationBuilder.Sql(
                """
                CREATE OR ALTER TRIGGER [TR_tbl_ContractNegotiationComment_AppendOnly]
                ON [dbo].[tbl_ContractNegotiationComment]
                AFTER UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF EXISTS
                    (
                        SELECT 1
                        FROM [deleted] d
                        LEFT JOIN [inserted] i ON i.[CommentId] = d.[CommentId]
                        WHERE i.[CommentId] IS NULL
                           OR i.[ContractId] <> d.[ContractId]
                           OR i.[VersionId] <> d.[VersionId]
                           OR ISNULL(i.[TermId], 0) <> ISNULL(d.[TermId], 0)
                           OR ISNULL(i.[ParentCommentId], 0) <> ISNULL(d.[ParentCommentId], 0)
                           OR i.[Content] <> d.[Content]
                           OR i.[Source] <> d.[Source]
                           OR i.[RecordedByEmployeeId] <> d.[RecordedByEmployeeId]
                           OR i.[CreatedDate] <> d.[CreatedDate]
                    )
                    BEGIN
                        ROLLBACK TRANSACTION;
                        THROW 51005, N'Negotiation comments are append-only.', 1;
                    END;
                END;
                """);

            migrationBuilder.Sql(
                """
                CREATE OR ALTER TRIGGER [TR_tbl_ContractNegotiationCommentEvent_AppendOnly]
                ON [dbo].[tbl_ContractNegotiationCommentEvent]
                AFTER UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF EXISTS
                    (
                        SELECT 1
                        FROM [deleted] d
                        LEFT JOIN [inserted] i ON i.[CommentEventId] = d.[CommentEventId]
                        WHERE i.[CommentEventId] IS NULL
                           OR i.[CommentId] <> d.[CommentId]
                           OR i.[EventType] <> d.[EventType]
                           OR i.[EmployeeId] <> d.[EmployeeId]
                           OR i.[OccurredAt] <> d.[OccurredAt]
                    )
                    BEGIN
                        ROLLBACK TRANSACTION;
                        THROW 51006, N'Negotiation comment events are append-only.', 1;
                    END;
                END;
                """);
        }
    }
}
