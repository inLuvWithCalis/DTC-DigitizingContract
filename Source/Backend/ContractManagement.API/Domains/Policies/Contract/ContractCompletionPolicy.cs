using ContractManagement.API.Common.Enums;
using ContractManagement.API.Domains.Models.Contract;

namespace ContractManagement.API.Domains.Policies.Contract
{
    /// <summary>
    /// Đánh giá hợp đồng có đủ điều kiện chuyển từ Signed sang Completed hay không.
    ///
    /// Policy này không tự cập nhật database.
    /// Nó chỉ kiểm tra và trả về toàn bộ các điều kiện còn thiếu.
    /// </summary>
    public static class ContractCompletionPolicy
    {
        /// <summary>
        /// Kiểm tra toàn bộ điều kiện hoàn thành hợp đồng.
        /// </summary>
        /// <param name="contractStatus">
        /// Trạng thái lifecycle hiện tại của hợp đồng.
        /// </param>
        /// <param name="providerSignatureStatus">
        /// Trạng thái chữ ký của đại diện DTC.
        /// </param>
        /// <param name="customerSignatureStatus">
        /// Trạng thái chữ ký của đại diện khách hàng.
        /// </param>
        /// <param name="hardCopyStatus">
        /// Trạng thái thu hồi và lưu kho bản cứng.
        /// </param>
        /// <param name="deliveryStatus">
        /// Trạng thái triển khai kỹ thuật.
        /// </param>
        /// <param name="paymentProgressStatus">
        /// Trạng thái thanh toán được tính từ các khoản thanh toán Confirmed.
        /// </param>
        /// <param name="missingRequiredDocuments">
        /// Danh sách tên các chứng từ bắt buộc còn thiếu.
        ///
        /// Danh sách rỗng nghĩa là đã đủ chứng từ.
        /// Việc xác định chứng từ nào bắt buộc sẽ do policy riêng xử lý
        /// theo từng loại hợp đồng trong bước sau.
        /// </param>
        public static ContractCompletionEvaluation Evaluate(
            ContractStatus contractStatus,
            SignatureStatus providerSignatureStatus,
            SignatureStatus customerSignatureStatus,
            HardCopyStatus hardCopyStatus,
            DeliveryStatus deliveryStatus,
            PaymentProgressStatus paymentProgressStatus,
            IEnumerable<string> missingRequiredDocuments)
        {
            ArgumentNullException.ThrowIfNull(missingRequiredDocuments);

            // Phát hiện sớm dữ liệu enum không hợp lệ,
            // tránh âm thầm coi dữ liệu lỗi là một trạng thái nghiệp vụ bình thường.
            EnsureDefined(contractStatus, nameof(contractStatus));
            EnsureDefined(providerSignatureStatus, nameof(providerSignatureStatus));
            EnsureDefined(customerSignatureStatus, nameof(customerSignatureStatus));
            EnsureDefined(hardCopyStatus, nameof(hardCopyStatus));
            EnsureDefined(deliveryStatus, nameof(deliveryStatus));
            EnsureDefined(paymentProgressStatus, nameof(paymentProgressStatus));

            var blockers = new List<ContractCompletionBlocker>();

            /*
             * Điều kiện 1:
             * Contract lifecycle phải đang ở Signed.
             *
             * Chỉ Signed mới được phép đi tiếp sang Completed.
             */
            if (contractStatus != ContractStatus.Signed)
            {
                blockers.Add(new ContractCompletionBlocker(
                    ContractCompletionBlockerCode.ContractMustBeSigned));
            }

            /*
             * Điều kiện 2 và 3:
             * Kiểm tra riêng chữ ký của hai bên.
             *
             * Mặc dù ContractStatus.Signed bình thường chỉ được tạo ra
             * sau khi hai bên ký xong, việc kiểm tra lại giúp bảo vệ hệ thống
             * trước dữ liệu sai hoặc dữ liệu bị cập nhật ngoài workflow.
             */
            if (providerSignatureStatus != SignatureStatus.Signed)
            {
                blockers.Add(new ContractCompletionBlocker(
                    ContractCompletionBlockerCode.ProviderSignatureMustBeSigned));
            }

            if (customerSignatureStatus != SignatureStatus.Signed)
            {
                blockers.Add(new ContractCompletionBlocker(
                    ContractCompletionBlockerCode.CustomerSignatureMustBeSigned));
            }

            /*
             * Điều kiện 4:
             * Bản cứng phải thực sự được thu hồi và lưu kho.
             *
             * Deployment Override chỉ cho phép đội kỹ thuật triển khai trước.
             * Override tuyệt đối không thay thế điều kiện Stored khi đóng hợp đồng.
             */
            if (hardCopyStatus != HardCopyStatus.Stored)
            {
                blockers.Add(new ContractCompletionBlocker(
                    ContractCompletionBlockerCode.HardCopyMustBeStored));
            }

            /*
             * Điều kiện 5:
             * Triển khai phải hoàn thành và đã được nghiệm thu.
             */
            if (deliveryStatus != DeliveryStatus.Accepted)
            {
                blockers.Add(new ContractCompletionBlocker(
                    ContractCompletionBlockerCode.DeliveryMustBeAccepted));
            }

            /*
             * Điều kiện 6:
             * Tổng các khoản thanh toán Confirmed phải đạt đủ số tiền yêu cầu.
             *
             * Trạng thái này được tính bởi PaymentPolicy,
             * không lấy trực tiếp từ một khoản thanh toán riêng lẻ.
             */
            if (paymentProgressStatus != PaymentProgressStatus.FullyPaid)
            {
                blockers.Add(new ContractCompletionBlocker(
                    ContractCompletionBlockerCode.PaymentMustBeFullyPaid));
            }

            /*
             * Điều kiện 7:
             * Kiểm tra các chứng từ bắt buộc còn thiếu.
             *
             * HashSet giúp loại bỏ tên chứng từ bị trùng,
             * nhưng vẫn giữ từng chứng từ thiếu thành một blocker riêng.
             */
            var normalizedDocumentNames =
                new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var documentName in missingRequiredDocuments)
            {
                // Bỏ qua dữ liệu rỗng vì nó không phải tên chứng từ hợp lệ.
                if (string.IsNullOrWhiteSpace(documentName))
                {
                    continue;
                }

                var normalizedName = documentName.Trim();

                if (!normalizedDocumentNames.Add(normalizedName))
                {
                    continue;
                }

                blockers.Add(new ContractCompletionBlocker(
                    ContractCompletionBlockerCode.RequiredDocumentMissing,
                    normalizedName));
            }

            return new ContractCompletionEvaluation(blockers);
        }

        /// <summary>
        /// Kiểm tra một giá trị có thực sự được định nghĩa trong enum hay không.
        /// </summary>
        private static void EnsureDefined<TEnum>(
            TEnum value,
            string parameterName)
            where TEnum : struct, Enum
        {
            if (!Enum.IsDefined(typeof(TEnum), value))
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    $"Unsupported {typeof(TEnum).Name} value.");
            }
        }
    }
}