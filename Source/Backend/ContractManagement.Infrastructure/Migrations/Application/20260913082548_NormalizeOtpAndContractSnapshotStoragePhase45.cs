using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractManagement.Infrastructure.Migrations.Application
{
    /// <inheritdoc />
    public partial class NormalizeOtpAndContractSnapshotStoragePhase45 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS (
                    SELECT 1
                    FROM [tbl_ContractVersion]
                    WHERE [IsLocked] = CAST(1 AS bit))
                BEGIN
                    THROW 51000, 'Phase 6 requires a fresh tenant database because locked JSON snapshots are intentionally not converted.', 1;
                END;

                DELETE FROM [tbl_ContractCustomerOtpDeliveryOutbox];
                """);

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractVersion_LockState",
                table: "tbl_ContractVersion");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractCustomerOtpDeliveryOutbox_Payload",
                table: "tbl_ContractCustomerOtpDeliveryOutbox");

            migrationBuilder.DropColumn(
                name: "SnapshotJson",
                table: "tbl_ContractVersion");

            migrationBuilder.DropColumn(
                name: "EncryptedPayload",
                table: "tbl_ContractCustomerOtpDeliveryOutbox");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveryExpiresAt",
                table: "tbl_ContractCustomerOtpDeliveryOutbox",
                type: "datetime2",
                nullable: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "EmailCiphertext",
                table: "tbl_ContractCustomerOtpDeliveryOutbox",
                type: "varbinary(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "OtpCiphertext",
                table: "tbl_ContractCustomerOtpDeliveryOutbox",
                type: "varbinary(128)",
                maxLength: 128,
                nullable: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "PhoneCiphertext",
                table: "tbl_ContractCustomerOtpDeliveryOutbox",
                type: "varbinary(256)",
                maxLength: 256,
                nullable: false);

            migrationBuilder.CreateTable(
                name: "tbl_ContractVersionLegalSnapshot",
                columns: table => new
                {
                    ContractVersionLegalSnapshotId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VersionId = table.Column<int>(type: "int", nullable: false),
                    ContractId = table.Column<int>(type: "int", nullable: false),
                    VersionNo = table.Column<int>(type: "int", nullable: false),
                    SourceVersionId = table.Column<int>(type: "int", nullable: true),
                    TemplateVersionId = table.Column<int>(type: "int", nullable: false),
                    ContractCode = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    ContractName = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ContractNameEn = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    ContractType = table.Column<byte>(type: "tinyint", nullable: false),
                    ContractCreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SignDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpireDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CurrencyCode = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false),
                    LanguageMode = table.Column<byte>(type: "tinyint", nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalDiscount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByEmployeeId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_ContractVersionLegalSnapshot", x => x.ContractVersionLegalSnapshotId);
                    table.CheckConstraint("CK_tbl_ContractVersionLegalSnapshot_Financials", "[Subtotal] >= 0 AND [TotalDiscount] >= 0 AND [TotalVat] >= 0 AND [TotalAmount] >= 0");
                    table.CheckConstraint("CK_tbl_ContractVersionLegalSnapshot_Ids", "[VersionId] > 0 AND [ContractId] > 0 AND [VersionNo] > 0 AND [TemplateVersionId] > 0 AND [CreatedByEmployeeId] > 0");
                    table.ForeignKey(
                        name: "FK_tbl_ContractVersionLegalSnapshot_tbl_ContractVersion_VersionId",
                        column: x => x.VersionId,
                        principalTable: "tbl_ContractVersion",
                        principalColumn: "VersionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbl_ContractVersionPartySnapshot",
                columns: table => new
                {
                    ContractVersionPartySnapshotId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractVersionLegalSnapshotId = table.Column<long>(type: "bigint", nullable: false),
                    PartyRole = table.Column<byte>(type: "tinyint", nullable: false),
                    SourceCustomerId = table.Column<int>(type: "int", nullable: true),
                    LegalName = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    TaxCode = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    RepresentativeName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RepresentativeTitle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PhoneNumber = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    FaxNumber = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    BankAccountNumber = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    BankName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_ContractVersionPartySnapshot", x => x.ContractVersionPartySnapshotId);
                    table.CheckConstraint("CK_tbl_ContractVersionPartySnapshot_Role", "[PartyRole] IN (1, 2)");
                    table.CheckConstraint("CK_tbl_ContractVersionPartySnapshot_Source", "([PartyRole] = 1 AND [SourceCustomerId] IS NULL) OR ([PartyRole] = 2 AND [SourceCustomerId] > 0)");
                    table.ForeignKey(
                        name: "FK_tbl_ContractVersionPartySnapshot_tbl_ContractVersionLegalSnapshot_ContractVersionLegalSnapshotId",
                        column: x => x.ContractVersionLegalSnapshotId,
                        principalTable: "tbl_ContractVersionLegalSnapshot",
                        principalColumn: "ContractVersionLegalSnapshotId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbl_ContractVersionPaymentMilestoneSnapshot",
                columns: table => new
                {
                    ContractVersionPaymentMilestoneSnapshotId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractVersionLegalSnapshotId = table.Column<long>(type: "bigint", nullable: false),
                    SourcePaymentMilestoneId = table.Column<int>(type: "int", nullable: false),
                    SourceTermId = table.Column<int>(type: "int", nullable: false),
                    SourceTemplatePaymentMilestoneId = table.Column<int>(type: "int", nullable: true),
                    MilestoneCode = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    TitleVi = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TitleEn = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    PaymentPercent = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    DueAnchor = table.Column<byte>(type: "tinyint", nullable: false),
                    DueOffsetDays = table.Column<int>(type: "int", nullable: false),
                    DayCountMode = table.Column<byte>(type: "tinyint", nullable: false),
                    ConditionVi = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ConditionEn = table.Column<string>(type: "varchar(2000)", unicode: false, maxLength: 2000, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AnchorDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaymentStatus = table.Column<byte>(type: "tinyint", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaidByEmployeeId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_ContractVersionPaymentMilestoneSnapshot", x => x.ContractVersionPaymentMilestoneSnapshotId);
                    table.CheckConstraint("CK_tbl_ContractVersionPaymentMilestoneSnapshot_Ids", "[SourcePaymentMilestoneId] > 0 AND [SourceTermId] > 0 AND ([SourceTemplatePaymentMilestoneId] IS NULL OR [SourceTemplatePaymentMilestoneId] > 0)");
                    table.CheckConstraint("CK_tbl_ContractVersionPaymentMilestoneSnapshot_Values", "[PaymentPercent] > 0 AND [PaymentPercent] <= 100 AND [Amount] >= 0 AND [DisplayOrder] > 0");
                    table.ForeignKey(
                        name: "FK_tbl_ContractVersionPaymentMilestoneSnapshot_tbl_ContractVersionLegalSnapshot_ContractVersionLegalSnapshotId",
                        column: x => x.ContractVersionLegalSnapshotId,
                        principalTable: "tbl_ContractVersionLegalSnapshot",
                        principalColumn: "ContractVersionLegalSnapshotId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractVersion_LockState",
                table: "tbl_ContractVersion",
                sql: "([IsLocked] = 0 AND [LockedDate] IS NULL AND [LockedByEmployeeId] IS NULL) OR ([IsLocked] = 1 AND [LockedDate] IS NOT NULL AND [LockedByEmployeeId] IS NOT NULL AND [SnapshotHash] IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractCustomerOtpDeliveryOutbox_Ciphertexts",
                table: "tbl_ContractCustomerOtpDeliveryOutbox",
                sql: "DATALENGTH([PhoneCiphertext]) >= 28 AND DATALENGTH([OtpCiphertext]) >= 28 AND ([EmailCiphertext] IS NULL OR DATALENGTH([EmailCiphertext]) >= 28)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractCustomerOtpDeliveryOutbox_Expiry",
                table: "tbl_ContractCustomerOtpDeliveryOutbox",
                sql: "[DeliveryExpiresAt] > [CreatedDate]");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractVersionLegalSnapshot_VersionId",
                table: "tbl_ContractVersionLegalSnapshot",
                column: "VersionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractVersionPartySnapshot_ContractVersionLegalSnapshotId_PartyRole",
                table: "tbl_ContractVersionPartySnapshot",
                columns: new[] { "ContractVersionLegalSnapshotId", "PartyRole" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractVersionPaymentMilestoneSnapshot_ContractVersionLegalSnapshotId_DisplayOrder",
                table: "tbl_ContractVersionPaymentMilestoneSnapshot",
                columns: new[] { "ContractVersionLegalSnapshotId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractVersionPaymentMilestoneSnapshot_ContractVersionLegalSnapshotId_SourcePaymentMilestoneId",
                table: "tbl_ContractVersionPaymentMilestoneSnapshot",
                columns: new[] { "ContractVersionLegalSnapshotId", "SourcePaymentMilestoneId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DELETE FROM [tbl_ContractCustomerOtpDeliveryOutbox];");

            migrationBuilder.DropTable(
                name: "tbl_ContractVersionPartySnapshot");

            migrationBuilder.DropTable(
                name: "tbl_ContractVersionPaymentMilestoneSnapshot");

            migrationBuilder.DropTable(
                name: "tbl_ContractVersionLegalSnapshot");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractVersion_LockState",
                table: "tbl_ContractVersion");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractCustomerOtpDeliveryOutbox_Ciphertexts",
                table: "tbl_ContractCustomerOtpDeliveryOutbox");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractCustomerOtpDeliveryOutbox_Expiry",
                table: "tbl_ContractCustomerOtpDeliveryOutbox");

            migrationBuilder.DropColumn(
                name: "DeliveryExpiresAt",
                table: "tbl_ContractCustomerOtpDeliveryOutbox");

            migrationBuilder.DropColumn(
                name: "EmailCiphertext",
                table: "tbl_ContractCustomerOtpDeliveryOutbox");

            migrationBuilder.DropColumn(
                name: "OtpCiphertext",
                table: "tbl_ContractCustomerOtpDeliveryOutbox");

            migrationBuilder.DropColumn(
                name: "PhoneCiphertext",
                table: "tbl_ContractCustomerOtpDeliveryOutbox");

            migrationBuilder.AddColumn<string>(
                name: "SnapshotJson",
                table: "tbl_ContractVersion",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EncryptedPayload",
                table: "tbl_ContractCustomerOtpDeliveryOutbox",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractVersion_LockState",
                table: "tbl_ContractVersion",
                sql: "([IsLocked] = 0 AND [LockedDate] IS NULL AND [LockedByEmployeeId] IS NULL) OR ([IsLocked] = 1 AND [LockedDate] IS NOT NULL AND [LockedByEmployeeId] IS NOT NULL AND [SnapshotJson] IS NOT NULL AND [SnapshotHash] IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractCustomerOtpDeliveryOutbox_Payload",
                table: "tbl_ContractCustomerOtpDeliveryOutbox",
                sql: "LEN(LTRIM(RTRIM([EncryptedPayload]))) > 0");
        }
    }
}
