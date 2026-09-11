using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractManagement.Infrastructure.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddContractPaymentMilestoneStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                table: "tbl_ContractPaymentMilestone",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaidByEmployeeId",
                table: "tbl_ContractPaymentMilestone",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "PaymentStatus",
                table: "tbl_ContractPaymentMilestone",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.Sql(
                """
                UPDATE milestone
                SET milestone.PaymentStatus = 1,
                    milestone.PaidAt = finalPayment.CreatedAt,
                    milestone.PaidByEmployeeId = finalPayment.CreatedByEmployeeId
                FROM tbl_ContractPaymentMilestone AS milestone
                CROSS APPLY
                (
                    SELECT TOP (1) payment.CreatedAt, payment.CreatedByEmployeeId
                    FROM tbl_ContractPaymentLedger AS payment
                    WHERE payment.PaymentMilestoneId = milestone.PaymentMilestoneId
                      AND payment.Status = 1
                    ORDER BY payment.PaymentDate DESC, payment.ContractPaymentId DESC
                ) AS finalPayment
                WHERE
                (
                    SELECT COALESCE(SUM(payment.Amount), 0)
                    FROM tbl_ContractPaymentLedger AS payment
                    WHERE payment.PaymentMilestoneId = milestone.PaymentMilestoneId
                      AND payment.Status = 1
                ) >= milestone.Amount;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractPaymentMilestone_PaidByEmployeeId",
                table: "tbl_ContractPaymentMilestone",
                column: "PaidByEmployeeId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractPaymentMilestone_PaidMetadata",
                table: "tbl_ContractPaymentMilestone",
                sql: "([PaymentStatus] = 0 AND [PaidAt] IS NULL AND [PaidByEmployeeId] IS NULL) OR ([PaymentStatus] = 1 AND [PaidAt] IS NOT NULL AND [PaidByEmployeeId] IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractPaymentMilestone_PaymentStatus",
                table: "tbl_ContractPaymentMilestone",
                sql: "[PaymentStatus] IN (0, 1)");

            migrationBuilder.AddForeignKey(
                name: "FK_tbl_ContractPaymentMilestone_tbl_Employee_PaidByEmployeeId",
                table: "tbl_ContractPaymentMilestone",
                column: "PaidByEmployeeId",
                principalTable: "tbl_Employee",
                principalColumn: "EmployeeId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tbl_ContractPaymentMilestone_tbl_Employee_PaidByEmployeeId",
                table: "tbl_ContractPaymentMilestone");

            migrationBuilder.DropIndex(
                name: "IX_tbl_ContractPaymentMilestone_PaidByEmployeeId",
                table: "tbl_ContractPaymentMilestone");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractPaymentMilestone_PaidMetadata",
                table: "tbl_ContractPaymentMilestone");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractPaymentMilestone_PaymentStatus",
                table: "tbl_ContractPaymentMilestone");

            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "tbl_ContractPaymentMilestone");

            migrationBuilder.DropColumn(
                name: "PaidByEmployeeId",
                table: "tbl_ContractPaymentMilestone");

            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                table: "tbl_ContractPaymentMilestone");
        }
    }
}
