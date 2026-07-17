using ContractManagement.API.Common.Enums;
using ContractManagement.API.Domains.Policies.Contract;

namespace ContractManagement.Tests.Domains.Policies.Contract
{
    /// <summary>
    /// Kiểm thử vòng đời của approval request
    /// và mối liên hệ với ContractStatus.
    /// </summary>
    public class ApprovalRequestPolicyTests
    {
        [Theory]
        [InlineData(ApprovalRequestStatus.Approved)]
        [InlineData(ApprovalRequestStatus.Returned)]
        [InlineData(ApprovalRequestStatus.Rejected)]
        [InlineData(ApprovalRequestStatus.Withdrawn)]
        public void Pending_ShouldTransitionToValidResult(
            ApprovalRequestStatus targetStatus)
        {
            // Act
            var result = ApprovalRequestPolicy.CanTransition(
                ApprovalRequestStatus.Pending,
                targetStatus);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData(ApprovalRequestStatus.Approved)]
        [InlineData(ApprovalRequestStatus.Returned)]
        [InlineData(ApprovalRequestStatus.Rejected)]
        [InlineData(ApprovalRequestStatus.Withdrawn)]
        public void TerminalStatus_ShouldNotTransitionAgain(
            ApprovalRequestStatus currentStatus)
        {
            // Act
            var result = ApprovalRequestPolicy.CanTransition(
                currentStatus,
                ApprovalRequestStatus.Pending);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData(
            ApprovalRequestStatus.Approved,
            ContractStatus.PendingSignature)]
        [InlineData(
            ApprovalRequestStatus.Returned,
            ContractStatus.Negotiating)]
        [InlineData(
            ApprovalRequestStatus.Rejected,
            ContractStatus.Rejected)]
        [InlineData(
            ApprovalRequestStatus.Withdrawn,
            ContractStatus.Negotiating)]
        public void GetTargetContractStatus_ShouldReturnExpectedStatus(
            ApprovalRequestStatus approvalResult,
            ContractStatus expectedContractStatus)
        {
            // Act
            var result =
                ApprovalRequestPolicy.GetTargetContractStatus(
                    approvalResult);

            // Assert
            Assert.Equal(expectedContractStatus, result);
        }

        [Fact]
        public void GetTargetContractStatus_ShouldThrow_WhenStillPending()
        {
            // Act
            var action = () =>
            {
                ApprovalRequestPolicy.GetTargetContractStatus(
                    ApprovalRequestStatus.Pending);
            };

            // Assert
            Assert.Throws<InvalidOperationException>(action);
        }


        [Fact]
        public void EnsureCanApplyResult_ShouldAllowValidApproval()
        {
            // Act
            var action = () =>
                ApprovalRequestPolicy.EnsureCanApplyResult(
                    ApprovalRequestStatus.Pending,
                    ApprovalRequestStatus.Approved,
                    ContractStatus.PendingApproval);

            // Assert: không phát sinh exception.
            var exception = Record.Exception(action);
            Assert.Null(exception);
        }

        [Fact]
        public void EnsureCanApplyResult_ShouldRejectWrongContractStatus()
        {
            // Hợp đồng đang Draft không thể nhận kết quả approval.
            var action = () =>
                ApprovalRequestPolicy.EnsureCanApplyResult(
                    ApprovalRequestStatus.Pending,
                    ApprovalRequestStatus.Approved,
                    ContractStatus.Draft);

            Assert.Throws<InvalidOperationException>(action);
        }

        [Fact]
        public void ApprovalRequestResult_ShouldOnlyBeAppliedOnce()
        {
            // Một request đã Approved không được Approved lần thứ hai.
            var action = () =>
                ApprovalRequestPolicy.EnsureCanTransition(
                    ApprovalRequestStatus.Approved,
                    ApprovalRequestStatus.Approved);

            Assert.Throws<InvalidOperationException>(action);
        }
    }
}