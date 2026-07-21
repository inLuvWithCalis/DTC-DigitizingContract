using ContractManagement.API.Common.Enums;
using ContractManagement.API.Domains.Models.Contract;
using ContractManagement.API.Domains.Policies.Contract;

namespace ContractManagement.Tests.Domains.Policies.Contract
{
    public class ContractCompletionPolicyTests
    {
        [Fact]
        public void Evaluate_WhenAllConditionsSatisfied_ShouldAllowCompletion()
        {
            var result = EvaluateReadyContract();

            Assert.True(result.CanComplete);
            Assert.Empty(result.Blockers);
        }

        [Fact]
        public void Evaluate_WhenContractIsNotSigned_ShouldReturnContractBlocker()
        {
            var result = EvaluateReadyContract(
                contractStatus: ContractStatus.PendingSignature);

            Assert.False(result.CanComplete);

            Assert.Contains(
                result.Blockers,
                x => x.Code ==
                     ContractCompletionBlockerCode.ContractMustBeSigned);
        }

        [Fact]
        public void Evaluate_WhenProviderHasNotSigned_ShouldReturnProviderBlocker()
        {
            var result = EvaluateReadyContract(
                providerSignatureStatus: SignatureStatus.Pending);

            Assert.False(result.CanComplete);

            Assert.Contains(
                result.Blockers,
                x => x.Code ==
                     ContractCompletionBlockerCode.ProviderSignatureMustBeSigned);
        }

        [Fact]
        public void Evaluate_WhenCustomerHasNotSigned_ShouldReturnCustomerBlocker()
        {
            var result = EvaluateReadyContract(
                customerSignatureStatus: SignatureStatus.Pending);

            Assert.False(result.CanComplete);

            Assert.Contains(
                result.Blockers,
                x => x.Code ==
                     ContractCompletionBlockerCode.CustomerSignatureMustBeSigned);
        }

        [Fact]
        public void Evaluate_WhenHardCopyIsNotStored_ShouldReturnHardCopyBlocker()
        {
            var result = EvaluateReadyContract(
                hardCopyStatus: HardCopyStatus.ReturnedSignedToCompany);

            Assert.False(result.CanComplete);

            Assert.Contains(
                result.Blockers,
                x => x.Code ==
                     ContractCompletionBlockerCode.HardCopyMustBeStored);
        }

        [Fact]
        public void Evaluate_WhenDeliveryIsNotAccepted_ShouldReturnDeliveryBlocker()
        {
            var result = EvaluateReadyContract(
                deliveryStatus: DeliveryStatus.InProgress);

            Assert.False(result.CanComplete);

            Assert.Contains(
                result.Blockers,
                x => x.Code ==
                     ContractCompletionBlockerCode.DeliveryMustBeAccepted);
        }

        [Fact]
        public void Evaluate_WhenPaymentIsNotFullyPaid_ShouldReturnPaymentBlocker()
        {
            var result = EvaluateReadyContract(
                paymentProgressStatus: PaymentProgressStatus.PartiallyPaid);

            Assert.False(result.CanComplete);

            Assert.Contains(
                result.Blockers,
                x => x.Code ==
                     ContractCompletionBlockerCode.PaymentMustBeFullyPaid);
        }

        [Fact]
        public void Evaluate_WhenRequiredDocumentsAreMissing_ShouldReturnEachDocument()
        {
            var result = EvaluateReadyContract(
                missingRequiredDocuments:
                [
                    "Biên bản thanh lý",
                    "Hóa đơn VAT"
                ]);

            Assert.False(result.CanComplete);

            var documentBlockers = result.Blockers
                .Where(x =>
                    x.Code ==
                    ContractCompletionBlockerCode.RequiredDocumentMissing)
                .ToList();

            Assert.Equal(2, documentBlockers.Count);

            Assert.Contains(
                documentBlockers,
                x => x.Reference == "Biên bản thanh lý");

            Assert.Contains(
                documentBlockers,
                x => x.Reference == "Hóa đơn VAT");
        }

        [Fact]
        public void Evaluate_ShouldReturnAllUnsatisfiedConditions_NotOnlyFirstOne()
        {
            var result = EvaluateReadyContract(
                contractStatus: ContractStatus.PendingSignature,
                providerSignatureStatus: SignatureStatus.Pending,
                customerSignatureStatus: SignatureStatus.Pending,
                hardCopyStatus: HardCopyStatus.Prepared,
                deliveryStatus: DeliveryStatus.Pending,
                paymentProgressStatus: PaymentProgressStatus.Pending,
                missingRequiredDocuments:
                [
                    "Biên bản nghiệm thu"
                ]);

            Assert.False(result.CanComplete);
            Assert.Equal(7, result.Blockers.Count);
        }

        [Fact]
        public void Evaluate_ShouldIgnoreBlankAndDuplicateDocumentNames()
        {
            var result = EvaluateReadyContract(
                missingRequiredDocuments:
                [
                    "",
                    "   ",
                    "Biên bản thanh lý",
                    " biên bản thanh lý "
                ]);

            var documentBlockers = result.Blockers
                .Where(x =>
                    x.Code ==
                    ContractCompletionBlockerCode.RequiredDocumentMissing)
                .ToList();

            Assert.Single(documentBlockers);
            Assert.Equal("Biên bản thanh lý", documentBlockers[0].Reference);
        }

        [Fact]
        public void Evaluate_WhenDocumentCollectionIsNull_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(() =>
                ContractCompletionPolicy.Evaluate(
                    ContractStatus.Signed,
                    SignatureStatus.Signed,
                    SignatureStatus.Signed,
                    HardCopyStatus.Stored,
                    DeliveryStatus.Accepted,
                    PaymentProgressStatus.FullyPaid,
                    null!));
        }

        [Fact]
        public void Evaluate_WhenEnumValueIsInvalid_ShouldThrow()
        {
            var invalidContractStatus = (ContractStatus)255;

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ContractCompletionPolicy.Evaluate(
                    invalidContractStatus,
                    SignatureStatus.Signed,
                    SignatureStatus.Signed,
                    HardCopyStatus.Stored,
                    DeliveryStatus.Accepted,
                    PaymentProgressStatus.FullyPaid,
                    Array.Empty<string>()));
        }

        /// <summary>
        /// Tạo một hợp đồng mặc định đã đáp ứng toàn bộ điều kiện.
        /// Mỗi test chỉ override đúng điều kiện mà nó muốn kiểm tra.
        /// </summary>
        private static ContractCompletionEvaluation EvaluateReadyContract(
            ContractStatus contractStatus = ContractStatus.Signed,
            SignatureStatus providerSignatureStatus = SignatureStatus.Signed,
            SignatureStatus customerSignatureStatus = SignatureStatus.Signed,
            HardCopyStatus hardCopyStatus = HardCopyStatus.Stored,
            DeliveryStatus deliveryStatus = DeliveryStatus.Accepted,
            PaymentProgressStatus paymentProgressStatus =
                PaymentProgressStatus.FullyPaid,
            IEnumerable<string>? missingRequiredDocuments = null)
        {
            return ContractCompletionPolicy.Evaluate(
                contractStatus,
                providerSignatureStatus,
                customerSignatureStatus,
                hardCopyStatus,
                deliveryStatus,
                paymentProgressStatus,
                missingRequiredDocuments ?? Array.Empty<string>());
        }
    }
}