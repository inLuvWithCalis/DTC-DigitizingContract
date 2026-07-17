using ContractManagement.API.Common.Enums;
using ContractManagement.API.Domains.Policies.Contract;

namespace ContractManagement.Tests.Domains.Policies.Contract
{
    /// <summary>
    /// Kiểm thử quy tắc ký hợp đồng Phase 6 MVP.
    /// </summary>
    public class SignaturePolicyTests
    {
        [Fact]
        public void Provider_ShouldSignFirst_WithVerifiedOtp()
        {
            var action = () => SignaturePolicy.EnsureCanSign(
                SignerParty.Provider,
                SignatureStatus.Pending,
                SignatureStatus.Pending,
                SignatureMethod.OtpElectronic,
                otpVerified: true,
                hasSignedScanAttachment: false,
                contractSigningVersionId: 2,
                signatureVersionId: 2);

            var exception = Record.Exception(action);

            Assert.Null(exception);
        }

        [Fact]
        public void OnlineSignature_ShouldFail_WhenOtpIsNotVerified()
        {
            var action = () => SignaturePolicy.EnsureCanSign(
                SignerParty.Provider,
                SignatureStatus.Pending,
                SignatureStatus.Pending,
                SignatureMethod.OtpElectronic,
                otpVerified: false,
                hasSignedScanAttachment: false,
                contractSigningVersionId: 2,
                signatureVersionId: 2);

            Assert.Throws<InvalidOperationException>(action);
        }

        [Fact]
        public void Customer_ShouldNotSignBeforeProvider()
        {
            var action = () => SignaturePolicy.EnsureCanSign(
                SignerParty.Customer,
                SignatureStatus.Pending,
                SignatureStatus.Pending,
                SignatureMethod.OtpElectronic,
                otpVerified: true,
                hasSignedScanAttachment: false,
                contractSigningVersionId: 2,
                signatureVersionId: 2);

            Assert.Throws<InvalidOperationException>(action);
        }

        [Fact]
        public void Customer_ShouldSignAfterProvider()
        {
            var action = () => SignaturePolicy.EnsureCanSign(
                SignerParty.Customer,
                SignatureStatus.Pending,
                SignatureStatus.Signed,
                SignatureMethod.OtpElectronic,
                otpVerified: true,
                hasSignedScanAttachment: false,
                contractSigningVersionId: 2,
                signatureVersionId: 2);

            var exception = Record.Exception(action);

            Assert.Null(exception);
        }

        [Fact]
        public void WetInkScan_ShouldFail_WhenAttachmentIsMissing()
        {
            var action = () => SignaturePolicy.EnsureCanSign(
                SignerParty.Provider,
                SignatureStatus.Pending,
                SignatureStatus.Pending,
                SignatureMethod.WetInkScan,
                otpVerified: false,
                hasSignedScanAttachment: false,
                contractSigningVersionId: 2,
                signatureVersionId: 2);

            Assert.Throws<InvalidOperationException>(action);
        }

        [Fact]
        public void WetInkScan_ShouldNotRequireOtp_WhenAttachmentExists()
        {
            var action = () => SignaturePolicy.EnsureCanSign(
                SignerParty.Provider,
                SignatureStatus.Pending,
                SignatureStatus.Pending,
                SignatureMethod.WetInkScan,
                otpVerified: false,
                hasSignedScanAttachment: true,
                contractSigningVersionId: 2,
                signatureVersionId: 2);

            var exception = Record.Exception(action);

            Assert.Null(exception);
        }

        [Fact]
        public void Signing_ShouldFail_WhenVersionDoesNotMatch()
        {
            var action = () => SignaturePolicy.EnsureCanSign(
                SignerParty.Provider,
                SignatureStatus.Pending,
                SignatureStatus.Pending,
                SignatureMethod.OtpElectronic,
                otpVerified: true,
                hasSignedScanAttachment: false,
                contractSigningVersionId: 3,
                signatureVersionId: 2);

            Assert.Throws<InvalidOperationException>(action);
        }

        [Fact]
        public void Contract_ShouldRemainPending_WhenOnlyProviderSigned()
        {
            var result =
                SignaturePolicy.GetContractStatusAfterSigning(
                    SignatureStatus.Signed,
                    SignatureStatus.Pending);

            Assert.Equal(
                ContractStatus.PendingSignature,
                result);
        }

        [Fact]
        public void Contract_ShouldBecomeSigned_WhenBothPartiesSigned()
        {
            var result =
                SignaturePolicy.GetContractStatusAfterSigning(
                    SignatureStatus.Signed,
                    SignatureStatus.Signed);

            Assert.Equal(ContractStatus.Signed, result);
        }

        [Fact]
        public void SignedSignature_ShouldNotBeInvalidated()
        {
            var result = SignaturePolicy.CanTransition(
                SignatureStatus.Signed,
                SignatureStatus.Invalidated);

            Assert.False(result);
        }

        [Fact]
        public void PendingSignature_CanBeInvalidated()
        {
            var result = SignaturePolicy.CanTransition(
                SignatureStatus.Pending,
                SignatureStatus.Invalidated);

            Assert.True(result);
        }

        [Fact]
        public void ContractShouldNotBecomeSigned_WhenCustomerHasNotSigned()
        {
            var action = () =>
                SignaturePolicy.EnsureCanMarkContractSigned(
                    ContractStatus.PendingSignature,
                    SignatureStatus.Signed,
                    SignatureStatus.Pending);

            Assert.Throws<InvalidOperationException>(action);
        }

        [Fact]
        public void ContractShouldBecomeSigned_WhenBothPartiesSigned()
        {
            var action = () =>
                SignaturePolicy.EnsureCanMarkContractSigned(
                    ContractStatus.PendingSignature,
                    SignatureStatus.Signed,
                    SignatureStatus.Signed);

            var exception = Record.Exception(action);

            Assert.Null(exception);
        }
    }
}