using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractManagement.Infrastructure.Migrations.Application
{
    /// <inheritdoc />
    public partial class EnforceActivePaymentPerMilestone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tbl_ContractPaymentLedger_PaymentMilestoneId",
                table: "tbl_ContractPaymentLedger");

            migrationBuilder.CreateIndex(
                name: "UX_tbl_ContractPaymentLedger_ActiveMilestone",
                table: "tbl_ContractPaymentLedger",
                column: "PaymentMilestoneId",
                unique: true,
                filter: "[PaymentMilestoneId] IS NOT NULL AND [Status] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_tbl_ContractPaymentLedger_ActiveMilestone",
                table: "tbl_ContractPaymentLedger");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractPaymentLedger_PaymentMilestoneId",
                table: "tbl_ContractPaymentLedger",
                column: "PaymentMilestoneId");
        }
    }
}
