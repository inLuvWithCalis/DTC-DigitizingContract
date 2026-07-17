using ContractManagement.API.Common.Enums;
using ContractManagement.API.Domains.Policies.Contract;
using Xunit;

namespace ContractManagement.Tests.Domains.Policies.Contract
{
    /// <summary>
    /// Kiểm thử các quy tắc vòng đời hợp đồng.
    /// </summary>
    public class ContractLifecyclePolicyTests
    {
        [Theory]
        [InlineData(
            ContractStatus.Draft,
            ContractStatus.Negotiating,
            true)]
        [InlineData(
            ContractStatus.Draft,
            ContractStatus.PendingApproval,
            false)]
        [InlineData(
            ContractStatus.Negotiating,
            ContractStatus.PendingApproval,
            true)]
        [InlineData(
            ContractStatus.Negotiating,
            ContractStatus.Rejected,
            false)]
        [InlineData(
            ContractStatus.PendingApproval,
            ContractStatus.Rejected,
            true)]
        [InlineData(
            ContractStatus.PendingApproval,
            ContractStatus.Negotiating,
            true)]
        [InlineData(
            ContractStatus.PendingApproval,
            ContractStatus.PendingSignature,
            true)]
        [InlineData(
            ContractStatus.PendingSignature,
            ContractStatus.Signed,
            true)]
        [InlineData(
            ContractStatus.Signed,
            ContractStatus.Completed,
            true)]
        [InlineData(
            ContractStatus.Signed,
            ContractStatus.Negotiating,
            false)]
        [InlineData(
            ContractStatus.Completed,
            ContractStatus.Draft,
            false)]
        [InlineData(
            ContractStatus.Cancelled,
            ContractStatus.Draft,
            false)]
        public void CanTransition_ShouldReturnExpectedResult(
            ContractStatus currentStatus,
            ContractStatus targetStatus,
            bool expected)
        {
            var result = ContractLifecyclePolicy.CanTransition(
                currentStatus,
                targetStatus);

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(ContractStatus.Draft, true)]
        [InlineData(ContractStatus.Negotiating, true)]
        [InlineData(ContractStatus.PendingApproval, false)]
        [InlineData(ContractStatus.PendingSignature, false)]
        [InlineData(ContractStatus.Signed, false)]
        [InlineData(ContractStatus.Completed, false)]
        [InlineData(ContractStatus.Cancelled, false)]
        [InlineData(ContractStatus.Rejected, false)]
        public void CanEditContent_ShouldFollowStageGate(
            ContractStatus status,
            bool expected)
        {
            var result =
                ContractLifecyclePolicy.CanEditContent(status);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void EnsureCanTransition_ShouldThrow_WhenTransitionIsInvalid()
        {
            var action = () =>
                ContractLifecyclePolicy.EnsureCanTransition(
                    ContractStatus.Signed,
                    ContractStatus.Negotiating);

            Assert.Throws<InvalidOperationException>(action);
        }

        [Theory]
        [InlineData(ContractStatus.Completed)]
        [InlineData(ContractStatus.Rejected)]
        [InlineData(ContractStatus.Cancelled)]
        public void IsTerminal_ShouldReturnTrue_ForTerminalStatus(
            ContractStatus status)
        {
            var result =
                ContractLifecyclePolicy.IsTerminal(status);

            Assert.True(result);
        }
    }
}