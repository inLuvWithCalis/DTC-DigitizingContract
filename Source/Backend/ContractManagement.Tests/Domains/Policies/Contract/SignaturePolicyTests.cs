using ContractManagement.API.Common.Enums;
using ContractManagement.API.Domains.Policies.Contract;

namespace ContractManagement.Tests.Domains.Policies.Contract;

public sealed class SignaturePolicyTests
{
    [Fact]
    public void PendingSignature_WithApprovedLockedVersion_CanUploadEvidence()
    {
        var exception = Record.Exception(() =>
            SignaturePolicy.EnsureCanUploadInitialEvidence(
                ContractStatus.PendingSignature,
                currentVersionId: 12,
                evidenceVersionId: 12,
                versionLocked: true,
                approvedArtifactsExist: true,
                activeEvidenceExists: false));

        Assert.Null(exception);
    }

    [Theory]
    [InlineData(ContractStatus.Negotiating)]
    [InlineData(ContractStatus.PendingApproval)]
    [InlineData(ContractStatus.Signed)]
    [InlineData(ContractStatus.Completed)]
    public void InitialEvidence_RejectsInvalidContractState(
        ContractStatus status)
    {
        Assert.Throws<InvalidOperationException>(() =>
            SignaturePolicy.EnsureCanUploadInitialEvidence(
                status,
                12,
                12,
                versionLocked: true,
                approvedArtifactsExist: true,
                activeEvidenceExists: false));
    }

    [Fact]
    public void InitialEvidence_RejectsVersionMismatch()
    {
        Assert.Throws<InvalidOperationException>(() =>
            SignaturePolicy.EnsureCanUploadInitialEvidence(
                ContractStatus.PendingSignature,
                12,
                13,
                versionLocked: true,
                approvedArtifactsExist: true,
                activeEvidenceExists: false));
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void InitialEvidence_RequiresLockedVersionAndApprovedArtifacts(
        bool versionLocked,
        bool approvedArtifactsExist)
    {
        Assert.Throws<InvalidOperationException>(() =>
            SignaturePolicy.EnsureCanUploadInitialEvidence(
                ContractStatus.PendingSignature,
                12,
                12,
                versionLocked,
                approvedArtifactsExist,
                activeEvidenceExists: false));
    }

    [Fact]
    public void InitialEvidence_RejectsSecondActiveEvidence()
    {
        Assert.Throws<InvalidOperationException>(() =>
            SignaturePolicy.EnsureCanUploadInitialEvidence(
                ContractStatus.PendingSignature,
                12,
                12,
                versionLocked: true,
                approvedArtifactsExist: true,
                activeEvidenceExists: true));
    }

    [Fact]
    public void SignedContract_CanSupersedeActiveEvidence()
    {
        var exception = Record.Exception(() =>
            SignaturePolicy.EnsureCanSupersedeEvidence(
                ContractStatus.Signed,
                12,
                12,
                versionLocked: true,
                approvedArtifactsExist: true,
                activeEvidenceExists: true));

        Assert.Null(exception);
    }

    [Theory]
    [InlineData(ContractStatus.PendingSignature)]
    [InlineData(ContractStatus.Completed)]
    [InlineData(ContractStatus.Cancelled)]
    public void Supersede_RejectsNonSignedContract(ContractStatus status)
    {
        Assert.Throws<InvalidOperationException>(() =>
            SignaturePolicy.EnsureCanSupersedeEvidence(
                status,
                12,
                12,
                versionLocked: true,
                approvedArtifactsExist: true,
                activeEvidenceExists: true));
    }
}
