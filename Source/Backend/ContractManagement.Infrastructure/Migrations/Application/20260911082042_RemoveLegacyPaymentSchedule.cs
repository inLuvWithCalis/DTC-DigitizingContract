using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractManagement.Infrastructure.Migrations.Application
{
    /// <inheritdoc />
    public partial class RemoveLegacyPaymentSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM tbl_ContractTemplateField
                WHERE PlaceholderKey = 'PAYMENT_SCHEDULE_TABLE';

                DELETE FROM tbl_ContractPlaceholderDefinition
                WHERE PlaceholderKey = 'PAYMENT_SCHEDULE_TABLE';
                """);

            migrationBuilder.DropTable(
                name: "tbl_PaymentSchedule");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tbl_PaymentSchedule",
                columns: table => new
                {
                    ScheduleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Amount = table.Column<double>(type: "float", nullable: false),
                    ContractId = table.Column<int>(type: "int", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PaidAmount = table.Column<double>(type: "float", nullable: false),
                    PaymentStatus = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false, defaultValue: "Pending")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__tbl_Paym__9C8A5B49EE0F0668", x => x.ScheduleId);
                });
        }
    }
}
