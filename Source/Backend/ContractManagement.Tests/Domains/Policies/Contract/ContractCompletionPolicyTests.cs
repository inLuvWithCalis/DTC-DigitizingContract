using ContractManagement.API.Common.Enums;
using ContractManagement.API.Domains.Policies.Contract;

namespace ContractManagement.Tests.Domains.Policies.Contract;

public sealed class ContractCompletionPolicyTests
{
    [Fact]
    public void Evaluate_returns_ready_when_all_phase10_conditions_are_met()
    {
        var result = ContractCompletionPolicy.Evaluate(ContractStatus.Signed, true, true, 100m, 100m);
        Assert.True(result.CanComplete);
        Assert.Empty(result.Blockers);
    }

    [Theory]
    [InlineData(ContractStatus.PendingSignature, false, true, 100, 100, ContractCompletionBlockerCode.ContractMustBeSigned)]
    [InlineData(ContractStatus.Signed, true, false, 100, 100, ContractCompletionBlockerCode.AcceptanceEvidenceMissing)]
    [InlineData(ContractStatus.Signed, true, true, 100, 99, ContractCompletionBlockerCode.PaymentNotFullyPaid)]
    public void Evaluate_returns_stable_blocker(ContractStatus status, bool signed,
        bool acceptance, decimal total, decimal paid, ContractCompletionBlockerCode expected)
    {
        var result = ContractCompletionPolicy.Evaluate(status, signed, acceptance, total, paid);
        Assert.False(result.CanComplete);
        Assert.Contains(result.Blockers, blocker => blocker.Code == expected);
    }

    [Fact]
    public void Evaluate_reports_all_missing_conditions()
    {
        var result = ContractCompletionPolicy.Evaluate(ContractStatus.PendingSignature, false, false, 100m, 0m);
        Assert.Equal(3, result.Blockers.Count);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(100, -1)]
    [InlineData(100, 101)]
    public void Evaluate_rejects_invalid_amounts(decimal total, decimal paid)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ContractCompletionPolicy.Evaluate(ContractStatus.Signed, true, true, total, paid));
    }
}
