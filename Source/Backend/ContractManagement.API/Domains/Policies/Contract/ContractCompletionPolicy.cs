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
        /// <param name="hasActiveSignedEvidence">Có bản scan đã ký đang hiệu lực.</param>
        /// <param name="hasAcceptanceEvidence">Có biên bản nghiệm thu.</param>
        /// <param name="totalAmount">Giá trị contract version đã ký.</param>
        /// <param name="paidAmount">Tổng thanh toán chưa bị void.</param>
        public static ContractCompletionEvaluation Evaluate(
            ContractStatus contractStatus,
            bool hasActiveSignedEvidence,
            bool hasAcceptanceEvidence,
            decimal totalAmount,
            decimal paidAmount)
        {
            EnsureDefined(contractStatus, nameof(contractStatus));
            if (totalAmount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(totalAmount));
            }
            if (paidAmount < 0 || paidAmount > totalAmount)
            {
                throw new ArgumentOutOfRangeException(nameof(paidAmount));
            }

            var blockers = new List<ContractCompletionBlocker>();

            /*
             * Điều kiện 1:
             * Contract lifecycle phải đang ở Signed.
             *
             * Chỉ Signed mới được phép đi tiếp sang Completed.
             */
            if (contractStatus != ContractStatus.Signed
                || !hasActiveSignedEvidence)
            {
                blockers.Add(new ContractCompletionBlocker(
                    ContractCompletionBlockerCode.ContractMustBeSigned));
            }
            if (!hasAcceptanceEvidence)
            {
                blockers.Add(new ContractCompletionBlocker(
                    ContractCompletionBlockerCode.AcceptanceEvidenceMissing));
            }
            if (paidAmount != totalAmount)
            {
                blockers.Add(new ContractCompletionBlocker(
                    ContractCompletionBlockerCode.PaymentNotFullyPaid));
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
