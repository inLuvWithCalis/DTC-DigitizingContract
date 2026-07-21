using ContractManagement.API.Common.Enums;
using ContractManagement.API.Domains.Policies.Contract;

namespace ContractManagement.Tests.Domains.Policies.Contract
{
    public class HardCopyPolicyTests
    {
        [Theory]
        [InlineData(
            HardCopyStatus.NotPrepared,
            HardCopyStatus.Prepared)]
        [InlineData(
            HardCopyStatus.Prepared,
            HardCopyStatus.SentToCustomer)]
        [InlineData(
            HardCopyStatus.SentToCustomer,
            HardCopyStatus.CustomerReceived)]
        [InlineData(
            HardCopyStatus.CustomerReceived,
            HardCopyStatus.ReturnedSignedToCompany)]
        [InlineData(
            HardCopyStatus.ReturnedSignedToCompany,
            HardCopyStatus.Stored)]
        public void HardCopy_ShouldFollowExpectedWorkflow(
            HardCopyStatus currentStatus,
            HardCopyStatus targetStatus)
        {
            var result = HardCopyPolicy.CanTransition(
                currentStatus,
                targetStatus);

            Assert.True(result);
        }

        [Fact]
        public void HardCopy_ShouldNotSkipDirectlyToStored()
        {
            var result = HardCopyPolicy.CanTransition(
                HardCopyStatus.SentToCustomer,
                HardCopyStatus.Stored);

            Assert.False(result);
        }

        [Fact]
        public void Stored_ShouldBeTerminal()
        {
            Assert.True(
                HardCopyPolicy.IsTerminal(HardCopyStatus.Stored));
        }
    }
}