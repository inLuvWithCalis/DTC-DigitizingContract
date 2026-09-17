using ContractManagement.API.Common.Enums;
using ContractManagement.Domains.Policies.Contract;

namespace ContractManagement.Tests.Domains.Policies.Contract;

public sealed class AcceptanceRecordPolicyTests
{
    [Theory]
    [InlineData(AcceptanceRecordStatus.Draft, AcceptanceRecordStatus.Finalized)]
    [InlineData(AcceptanceRecordStatus.Draft, AcceptanceRecordStatus.Cancelled)]
    [InlineData(AcceptanceRecordStatus.Finalized, AcceptanceRecordStatus.Signed)]
    [InlineData(AcceptanceRecordStatus.Finalized, AcceptanceRecordStatus.Cancelled)]
    public void CanTransition_AllowsDefinedEdges(
        AcceptanceRecordStatus source,
        AcceptanceRecordStatus target)
    {
        Assert.True(AcceptanceRecordPolicy.CanTransition(source, target));
    }

    [Theory]
    [InlineData(AcceptanceRecordStatus.Draft, AcceptanceRecordStatus.Signed)]
    [InlineData(AcceptanceRecordStatus.Signed, AcceptanceRecordStatus.Cancelled)]
    [InlineData(AcceptanceRecordStatus.Cancelled, AcceptanceRecordStatus.Draft)]
    public void CanTransition_RejectsUndefinedEdges(
        AcceptanceRecordStatus source,
        AcceptanceRecordStatus target)
    {
        Assert.False(AcceptanceRecordPolicy.CanTransition(source, target));
    }

    [Fact]
    public void EnsureCanCreate_RequiresSignedContractAndBusinessSignDate()
    {
        Assert.Throws<InvalidOperationException>(() =>
            AcceptanceRecordPolicy.EnsureCanCreate(true, null));
        AcceptanceRecordPolicy.EnsureCanCreate(true, new DateTime(2026, 9, 16));
    }
}
