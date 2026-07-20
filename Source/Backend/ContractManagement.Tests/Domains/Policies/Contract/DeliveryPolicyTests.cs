using ContractManagement.API.Common.Enums;
using ContractManagement.API.Domains.Policies.Contract;

namespace ContractManagement.Tests.Domains.Policies.Contract
{
    public class DeliveryPolicyTests
    {
        [Fact]
        public void Deployment_ShouldStart_WhenHardCopyStored()
        {
            var action = () =>
                DeliveryPolicy.EnsureCanStartDeployment(
                    ContractStatus.Signed,
                    DeliveryStatus.Pending,
                    HardCopyStatus.Stored,
                    hasTechnicalAssignment: true,
                    hasActiveBossOverride: false);

            var exception = Record.Exception(action);

            Assert.Null(exception);
        }

        [Fact]
        public void Deployment_ShouldStart_WithActiveBossOverride()
        {
            var action = () =>
                DeliveryPolicy.EnsureCanStartDeployment(
                    ContractStatus.Signed,
                    DeliveryStatus.Pending,
                    HardCopyStatus.SentToCustomer,
                    hasTechnicalAssignment: true,
                    hasActiveBossOverride: true);

            var exception = Record.Exception(action);

            Assert.Null(exception);
        }

        [Fact]
        public void Deployment_ShouldFail_WithoutHardCopyOrOverride()
        {
            var action = () =>
                DeliveryPolicy.EnsureCanStartDeployment(
                    ContractStatus.Signed,
                    DeliveryStatus.Pending,
                    HardCopyStatus.SentToCustomer,
                    hasTechnicalAssignment: true,
                    hasActiveBossOverride: false);

            Assert.Throws<InvalidOperationException>(action);
        }

        [Fact]
        public void Deployment_ShouldFail_WhenContractNotSigned()
        {
            var action = () =>
                DeliveryPolicy.EnsureCanStartDeployment(
                    ContractStatus.PendingSignature,
                    DeliveryStatus.Pending,
                    HardCopyStatus.Stored,
                    hasTechnicalAssignment: true,
                    hasActiveBossOverride: false);

            Assert.Throws<InvalidOperationException>(action);
        }

        [Fact]
        public void Deployment_ShouldFail_WithoutTechnicalAssignment()
        {
            var action = () =>
                DeliveryPolicy.EnsureCanStartDeployment(
                    ContractStatus.Signed,
                    DeliveryStatus.Pending,
                    HardCopyStatus.Stored,
                    hasTechnicalAssignment: false,
                    hasActiveBossOverride: false);

            Assert.Throws<InvalidOperationException>(action);
        }

        [Fact]
        public void Delivery_ShouldNotSkipFromPendingToAccepted()
        {
            var result = DeliveryPolicy.CanTransition(
                DeliveryStatus.Pending,
                DeliveryStatus.Accepted);

            Assert.False(result);
        }

        [Fact]
        public void Delivery_ShouldRequireAcceptanceRecord()
        {
            var action = () =>
                DeliveryPolicy.EnsureCanAccept(
                    DeliveryStatus.InProgress,
                    hasAcceptanceRecord: false);

            Assert.Throws<InvalidOperationException>(action);
        }

        [Fact]
        public void Delivery_ShouldBecomeAccepted_WhenRecordExists()
        {
            var action = () =>
                DeliveryPolicy.EnsureCanAccept(
                    DeliveryStatus.InProgress,
                    hasAcceptanceRecord: true);

            var exception = Record.Exception(action);

            Assert.Null(exception);
        }
    }
}