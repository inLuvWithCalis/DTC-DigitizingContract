using ContractManagement.API.Common.Enums;
using ContractManagement.API.Domains.Policies.Contract;

namespace ContractManagement.Tests.Domains.Policies.Contract
{
    public class PaymentPolicyTests
    {
        [Fact]
        public void PendingPayment_ShouldBeConfirmed()
        {
            var result = PaymentPolicy.CanTransition(
                PaymentRecordStatus.Pending,
                PaymentRecordStatus.Confirmed);

            Assert.True(result);
        }

        [Fact]
        public void ConfirmedPayment_ShouldBeVoided_WhenEnteredIncorrectly()
        {
            var result = PaymentPolicy.CanTransition(
                PaymentRecordStatus.Confirmed,
                PaymentRecordStatus.Voided);

            Assert.True(result);
        }

        [Fact]
        public void VoidedPayment_ShouldNotBeReactivated()
        {
            var result = PaymentPolicy.CanTransition(
                PaymentRecordStatus.Voided,
                PaymentRecordStatus.Confirmed);

            Assert.False(result);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void PaymentAmount_ShouldBeGreaterThanZero(
            decimal amount)
        {
            var action = () =>
                PaymentPolicy.EnsureValidAmount(amount);

            Assert.Throws<ArgumentOutOfRangeException>(action);
        }

        [Fact]
        public void Summary_ShouldOnlyCountConfirmedPayments()
        {
            var payments = new[]
            {
                (
                    Amount: 30_000_000m,
                    Status: PaymentRecordStatus.Confirmed
                ),
                (
                    Amount: 20_000_000m,
                    Status: PaymentRecordStatus.Pending
                ),
                (
                    Amount: 10_000_000m,
                    Status: PaymentRecordStatus.Voided
                )
            };

            var result = PaymentPolicy.CalculateSummary(
                100_000_000m,
                payments);

            Assert.Equal(30_000_000m, result.ConfirmedAmount);
            Assert.Equal(70_000_000m, result.OutstandingAmount);
            Assert.Equal(0m, result.OverpaidAmount);
            Assert.Equal(
                PaymentProgressStatus.PartiallyPaid,
                result.ProgressStatus);
        }

        [Fact]
        public void NoConfirmedPayment_ShouldBePending()
        {
            var payments = new[]
            {
                (
                    Amount: 20_000_000m,
                    Status: PaymentRecordStatus.Pending
                )
            };

            var result = PaymentPolicy.CalculateSummary(
                100_000_000m,
                payments);

            Assert.Equal(
                PaymentProgressStatus.Pending,
                result.ProgressStatus);

            Assert.Equal(0m, result.ConfirmedAmount);
        }

        [Fact]
        public void PartialPayment_ShouldBePartiallyPaid()
        {
            var payments = new[]
            {
                (
                    Amount: 40_000_000m,
                    Status: PaymentRecordStatus.Confirmed
                )
            };

            var result = PaymentPolicy.CalculateSummary(
                100_000_000m,
                payments);

            Assert.Equal(
                PaymentProgressStatus.PartiallyPaid,
                result.ProgressStatus);
        }

        [Fact]
        public void ExactPayment_ShouldBeFullyPaid()
        {
            var payments = new[]
            {
                (
                    Amount: 100_000_000m,
                    Status: PaymentRecordStatus.Confirmed
                )
            };

            var result = PaymentPolicy.CalculateSummary(
                100_000_000m,
                payments);

            Assert.Equal(
                PaymentProgressStatus.FullyPaid,
                result.ProgressStatus);

            Assert.Equal(0m, result.OutstandingAmount);
            Assert.Equal(0m, result.OverpaidAmount);
        }

        [Fact]
        public void Overpayment_ShouldStillBeFullyPaid()
        {
            var payments = new[]
            {
                (
                    Amount: 110_000_000m,
                    Status: PaymentRecordStatus.Confirmed
                )
            };

            var result = PaymentPolicy.CalculateSummary(
                100_000_000m,
                payments);

            Assert.Equal(
                PaymentProgressStatus.FullyPaid,
                result.ProgressStatus);

            Assert.Equal(0m, result.OutstandingAmount);
            Assert.Equal(10_000_000m, result.OverpaidAmount);
        }

        [Fact]
        public void UnpaidSchedule_ShouldBeOverdue_AfterDueDate()
        {
            var now = new DateTime(
                2026, 7, 17,
                8, 0, 0,
                DateTimeKind.Utc);

            var dueDate = now.AddDays(-1);

            var result = PaymentPolicy.IsOverdue(
                dueDate,
                PaymentProgressStatus.PartiallyPaid,
                now);

            Assert.True(result);
        }

        [Fact]
        public void FullyPaidSchedule_ShouldNotBeOverdue()
        {
            var now = new DateTime(
                2026, 7, 17,
                8, 0, 0,
                DateTimeKind.Utc);

            var dueDate = now.AddDays(-1);

            var result = PaymentPolicy.IsOverdue(
                dueDate,
                PaymentProgressStatus.FullyPaid,
                now);

            Assert.False(result);
        }

        [Fact]
        public void Summary_ShouldRejectInvalidRequiredAmount()
        {
            var payments = Array.Empty<(
                decimal Amount,
                PaymentRecordStatus Status)>();

            var action = () =>
                PaymentPolicy.CalculateSummary(
                    requiredAmount: 0,
                    payments);

            Assert.Throws<ArgumentOutOfRangeException>(action);
        }
    }
}