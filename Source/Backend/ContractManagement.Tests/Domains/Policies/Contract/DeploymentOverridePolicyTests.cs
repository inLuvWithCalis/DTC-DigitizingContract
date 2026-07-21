using ContractManagement.API.Common.Enums;
using ContractManagement.API.Domains.Policies.Contract;

namespace ContractManagement.Tests.Domains.Policies.Contract
{
    public class DeploymentOverridePolicyTests
    {
        [Fact]
        public void OverrideRequest_ShouldRequireReason()
        {
            var action = () =>
                DeploymentOverridePolicy.EnsureCanCreateRequest(" ");

            Assert.Throws<ArgumentException>(action);
        }

        [Fact]
        public void Manager_ShouldApprovePendingOverride()
        {
            var action = () =>
                DeploymentOverridePolicy.EnsureCanDecide(
                    DeploymentOverrideStatus.Pending,
                    DeploymentOverrideStatus.Approved,
                    EmployeeType.Manager,
                    "Khách đã ký và thanh toán đầy đủ.");

            var exception = Record.Exception(action);

            Assert.Null(exception);
        }

        [Fact]
        public void NonManager_ShouldNotApproveOverride()
        {
            var action = () =>
                DeploymentOverridePolicy.EnsureCanDecide(
                    DeploymentOverrideStatus.Pending,
                    DeploymentOverrideStatus.Approved,
                    EmployeeType.AdminOfficer,
                    "Đề nghị triển khai sớm.");

            Assert.Throws<UnauthorizedAccessException>(action);
        }

        [Fact]
        public void ApprovedOverride_ShouldBeActiveBeforeExpiry()
        {
            var now = new DateTime(
                2026, 7, 17,
                8, 0, 0,
                DateTimeKind.Utc);

            var expiresAt = now.AddHours(2);

            var result = DeploymentOverridePolicy.IsActive(
                DeploymentOverrideStatus.Approved,
                expiresAt,
                now);

            Assert.True(result);
        }

        [Fact]
        public void ApprovedOverride_ShouldBeInactiveAfterExpiry()
        {
            var now = new DateTime(
                2026, 7, 17,
                8, 0, 0,
                DateTimeKind.Utc);

            var expiresAt = now.AddMinutes(-1);

            var result = DeploymentOverridePolicy.IsActive(
                DeploymentOverrideStatus.Approved,
                expiresAt,
                now);

            Assert.False(result);
        }

        [Fact]
        public void RejectedOverride_ShouldNotBeActive()
        {
            var result = DeploymentOverridePolicy.IsActive(
                DeploymentOverrideStatus.Rejected,
                expiresAt: null,
                DateTime.UtcNow);

            Assert.False(result);
        }
    }
}