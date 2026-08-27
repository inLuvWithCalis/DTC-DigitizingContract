using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractManagement.Infrastructure.Migrations.Application
{
    /// <inheritdoc />
    public partial class ContractAuditTraceabilitySnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ActorDisplayNameSnapshot",
                table: "tbl_ContractAudit",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActorMaskedPhoneSnapshot",
                table: "tbl_ContractAudit",
                type: "varchar(32)",
                unicode: false,
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActorPhoneSourceSnapshot",
                table: "tbl_ContractAudit",
                type: "varchar(32)",
                unicode: false,
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContractCodeSnapshot",
                table: "tbl_ContractAudit",
                type: "varchar(50)",
                unicode: false,
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContractNameSnapshot",
                table: "tbl_ContractAudit",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VersionNoSnapshot",
                table: "tbl_ContractAudit",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(
                """
                DISABLE TRIGGER [TR_tbl_ContractAudit_AppendOnly]
                    ON [tbl_ContractAudit];

                UPDATE audit
                SET
                    [ActorDisplayNameSnapshot] = CASE
                        WHEN audit.[ActorType] = 'Employee' THEN COALESCE(
                            NULLIF(LTRIM(RTRIM(employee.[EmployeeFullName])), ''),
                            NULLIF(LTRIM(RTRIM(employee.[EmployeeCode])), ''),
                            NULLIF(LTRIM(RTRIM(employee.[EmployeeAccount])), ''))
                        WHEN audit.[ActorType] = 'Customer' THEN COALESCE(
                            NULLIF(LTRIM(RTRIM(customer.[CustomerFullName])), ''),
                            NULLIF(LTRIM(RTRIM(customer.[CustomerCompany])), ''),
                            NULLIF(LTRIM(RTRIM(customer.[CustomerRepresentativeName])), ''),
                            NULLIF(LTRIM(RTRIM(customer.[CustomerCode])), ''))
                        ELSE NULL
                    END,
                    [ActorMaskedPhoneSnapshot] = CASE
                        WHEN audit.[ActorType] = 'Customer'
                            AND LEN(phone.[PhoneNumberNormalized]) > 4
                        THEN REPLICATE('*', LEN(phone.[PhoneNumberNormalized]) - 4)
                            + RIGHT(phone.[PhoneNumberNormalized], 4)
                        WHEN audit.[ActorType] = 'Customer'
                        THEN phone.[PhoneNumberNormalized]
                        ELSE NULL
                    END,
                    [ActorPhoneSourceSnapshot] = CASE
                        WHEN audit.[ActorType] = 'Customer' THEN phone.[PhoneSource]
                        ELSE NULL
                    END,
                    [ContractCodeSnapshot] = contract.[ContractCode],
                    [ContractNameSnapshot] = contract.[ContractName],
                    [VersionNoSnapshot] = version.[VersionNo]
                FROM [tbl_ContractAudit] AS audit
                LEFT JOIN [tbl_Employee] AS employee
                    ON employee.[EmployeeId] = audit.[ActorEmployeeId]
                LEFT JOIN [tbl_Contract] AS contract
                    ON contract.[ContractId] = audit.[ContractId]
                LEFT JOIN [tbl_Customers] AS customer
                    ON customer.[CustomerId] = contract.[CustomerId]
                LEFT JOIN [tbl_ContractVersion] AS version
                    ON version.[VersionId] = audit.[VersionId]
                    AND version.[ContractId] = audit.[ContractId]
                LEFT JOIN [tbl_ContractCustomerAccessSession] AS session
                    ON session.[CustomerAccessSessionId]
                        = audit.[ActorCustomerAccessSessionId]
                    AND session.[TenantId] = audit.[TenantId]
                LEFT JOIN [tbl_ContractCustomerVerificationPhone] AS phone
                    ON phone.[VerificationPhoneId] = session.[VerificationPhoneId];

                ENABLE TRIGGER [TR_tbl_ContractAudit_AppendOnly]
                    ON [tbl_ContractAudit];
                """);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractAudit_Tenant_CorrelationId",
                table: "tbl_ContractAudit",
                columns: new[] { "TenantId", "CorrelationId" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractAudit_Tenant_CustomerSession_OccurredAt",
                table: "tbl_ContractAudit",
                columns: new[] { "TenantId", "ActorCustomerAccessSessionId", "OccurredAt", "ContractAuditId" },
                descending: new[] { false, false, true, true },
                filter: "[ActorCustomerAccessSessionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractAudit_Tenant_Employee_OccurredAt",
                table: "tbl_ContractAudit",
                columns: new[] { "TenantId", "ActorEmployeeId", "OccurredAt", "ContractAuditId" },
                descending: new[] { false, false, true, true },
                filter: "[ActorEmployeeId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractAudit_Tenant_Failure_OccurredAt",
                table: "tbl_ContractAudit",
                columns: new[] { "TenantId", "FailureCode", "OccurredAt", "ContractAuditId" },
                descending: new[] { false, false, true, true },
                filter: "[FailureCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractAudit_Tenant_Subject_OccurredAt",
                table: "tbl_ContractAudit",
                columns: new[] { "TenantId", "SubjectType", "SubjectId", "OccurredAt", "ContractAuditId" },
                descending: new[] { false, false, false, true, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tbl_ContractAudit_Tenant_CorrelationId",
                table: "tbl_ContractAudit");

            migrationBuilder.DropIndex(
                name: "IX_tbl_ContractAudit_Tenant_CustomerSession_OccurredAt",
                table: "tbl_ContractAudit");

            migrationBuilder.DropIndex(
                name: "IX_tbl_ContractAudit_Tenant_Employee_OccurredAt",
                table: "tbl_ContractAudit");

            migrationBuilder.DropIndex(
                name: "IX_tbl_ContractAudit_Tenant_Failure_OccurredAt",
                table: "tbl_ContractAudit");

            migrationBuilder.DropIndex(
                name: "IX_tbl_ContractAudit_Tenant_Subject_OccurredAt",
                table: "tbl_ContractAudit");

            migrationBuilder.DropColumn(
                name: "ActorDisplayNameSnapshot",
                table: "tbl_ContractAudit");

            migrationBuilder.DropColumn(
                name: "ActorMaskedPhoneSnapshot",
                table: "tbl_ContractAudit");

            migrationBuilder.DropColumn(
                name: "ActorPhoneSourceSnapshot",
                table: "tbl_ContractAudit");

            migrationBuilder.DropColumn(
                name: "ContractCodeSnapshot",
                table: "tbl_ContractAudit");

            migrationBuilder.DropColumn(
                name: "ContractNameSnapshot",
                table: "tbl_ContractAudit");

            migrationBuilder.DropColumn(
                name: "VersionNoSnapshot",
                table: "tbl_ContractAudit");
        }
    }
}
