using ContractManagement.API.Common.Enums;

namespace ContractManagement.API.Domains.Policies.Contract
{
    /// <summary>
    /// Quản lý quy tắc ký hợp đồng hoặc phụ lục.
    ///
    /// Quy tắc MVP:
    /// - mỗi bên có một người ký;
    /// - DTC/Provider phải ký trước;
    /// - Customer ký sau;
    /// - hai bên phải ký cùng ContractVersion;
    /// - ký online bắt buộc OTP;
    /// - ký giấy/scan bắt buộc có file scan nhưng không cần OTP.
    /// </summary>
    public static class SignaturePolicy
    {
        /// <summary>
        /// Vòng đời của một signature record.
        /// </summary>
        private static readonly IReadOnlyDictionary<
            SignatureStatus,
            HashSet<SignatureStatus>> AllowedTransitions =
            new Dictionary<SignatureStatus, HashSet<SignatureStatus>>
            {
                [SignatureStatus.Pending] = new()
                {
                    SignatureStatus.Signed,
                    SignatureStatus.Declined,
                    SignatureStatus.Invalidated
                },

                /*
                 * Các trạng thái kết thúc.
                 *
                 * Signature đã Signed không chuyển sang Invalidated.
                 * Nếu hợp đồng có version mới, signature cũ vẫn được giữ
                 * và được phân biệt bằng ContractVersionId.
                 */
                [SignatureStatus.Signed] = new(),
                [SignatureStatus.Declined] = new(),
                [SignatureStatus.Invalidated] = new()
            };

        /// <summary>
        /// Kiểm tra một signature record có được chuyển trạng thái hay không.
        /// </summary>
        public static bool CanTransition(
            SignatureStatus currentStatus,
            SignatureStatus targetStatus)
        {
            return AllowedTransitions.TryGetValue(
                       currentStatus,
                       out var allowedTargets)
                   && allowedTargets.Contains(targetStatus);
        }

        /// <summary>
        /// Chặn khi signature bị chuyển trạng thái sai.
        /// </summary>
        public static void EnsureCanTransition(
            SignatureStatus currentStatus,
            SignatureStatus targetStatus)
        {
            if (CanTransition(currentStatus, targetStatus))
            {
                return;
            }

            throw new InvalidOperationException(
                $"Không thể chuyển chữ ký từ " +
                $"{currentStatus} sang {targetStatus}.");
        }

        /// <summary>
        /// Kiểm tra một bên có đủ điều kiện thực hiện ký hay không.
        /// </summary>
        /// <param name="signerParty">
        /// Bên đang thực hiện ký: Provider hoặc Customer.
        /// </param>
        /// <param name="signerStatus">
        /// Trạng thái signature record của bên đang ký.
        /// </param>
        /// <param name="providerSignatureStatus">
        /// Trạng thái chữ ký phía DTC.
        /// Dùng để kiểm tra DTC đã ký trước khách hàng hay chưa.
        /// </param>
        /// <param name="signatureMethod">
        /// Phương thức ký: OTP online hoặc ký giấy/scan.
        /// </param>
        /// <param name="otpVerified">
        /// OTP đã được xác thực thành công hay chưa.
        /// Chỉ áp dụng cho OtpElectronic.
        /// </param>
        /// <param name="hasSignedScanAttachment">
        /// Đã có file scan hợp đồng ký giấy hay chưa.
        /// Chỉ áp dụng cho WetInkScan.
        /// </param>
        /// <param name="contractSigningVersionId">
        /// Version hiện tại đang được hợp đồng đưa đi ký.
        /// </param>
        /// <param name="signatureVersionId">
        /// Version mà signature record đang liên kết.
        /// </param>
        public static void EnsureCanSign(
            SignerParty signerParty,
            SignatureStatus signerStatus,
            SignatureStatus providerSignatureStatus,
            SignatureMethod signatureMethod,
            bool otpVerified,
            bool hasSignedScanAttachment,
            int contractSigningVersionId,
            int signatureVersionId)
        {
            // 1. Chỉ signature đang Pending mới được thực hiện ký.
            EnsureCanTransition(
                signerStatus,
                SignatureStatus.Signed);

            // 2. Chữ ký phải thuộc đúng version hiện đang được đưa đi ký.
            if (contractSigningVersionId != signatureVersionId)
            {
                throw new InvalidOperationException(
                    "Không thể ký vì phiên bản chữ ký không khớp " +
                    "với phiên bản hợp đồng hiện đang được đưa đi ký.");
            }

            /*
             * 3. Customer chỉ được ký sau khi DTC đã ký.
             *
             * Provider không cần kiểm tra điều kiện này vì Provider
             * chính là bên phải thực hiện ký đầu tiên.
             */
            if (signerParty == SignerParty.Customer
                && providerSignatureStatus != SignatureStatus.Signed)
            {
                throw new InvalidOperationException(
                    "Khách hàng chỉ được ký sau khi phía DTC đã ký.");
            }

            // 4. Kiểm tra bằng chứng theo từng phương thức ký.
            switch (signatureMethod)
            {
                case SignatureMethod.OtpElectronic:
                    if (!otpVerified)
                    {
                        throw new InvalidOperationException(
                            "Ký online chỉ hợp lệ sau khi OTP " +
                            "được xác thực thành công.");
                    }

                    break;

                case SignatureMethod.WetInkScan:
                    if (!hasSignedScanAttachment)
                    {
                        throw new InvalidOperationException(
                            "Ký giấy/scan phải có file hợp đồng " +
                            "đã ký được đính kèm.");
                    }

                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(signatureMethod),
                        "Phương thức ký không hợp lệ.");
            }
        }

        /// <summary>
        /// Xác định ContractStatus sau khi cập nhật chữ ký.
        ///
        /// Chỉ khi cả DTC và Customer đều Signed thì hợp đồng
        /// mới được chuyển từ PendingSignature sang Signed.
        /// </summary>
        public static ContractStatus GetContractStatusAfterSigning(
            SignatureStatus providerSignatureStatus,
            SignatureStatus customerSignatureStatus)
        {
            var bothPartiesSigned =
                providerSignatureStatus == SignatureStatus.Signed
                && customerSignatureStatus == SignatureStatus.Signed;

            return bothPartiesSigned
                ? ContractStatus.Signed
                : ContractStatus.PendingSignature;
        }

        /// <summary>
        /// Kiểm tra hợp đồng có đủ điều kiện chuyển sang Signed hay chưa.
        /// </summary>
        public static void EnsureCanMarkContractSigned(
            ContractStatus currentContractStatus,
            SignatureStatus providerSignatureStatus,
            SignatureStatus customerSignatureStatus)
        {
            if (providerSignatureStatus != SignatureStatus.Signed)
            {
                throw new InvalidOperationException(
                    "Phía DTC chưa hoàn tất ký hợp đồng.");
            }

            if (customerSignatureStatus != SignatureStatus.Signed)
            {
                throw new InvalidOperationException(
                    "Khách hàng chưa hoàn tất ký hợp đồng.");
            }

            ContractLifecyclePolicy.EnsureCanTransition(
                currentContractStatus,
                ContractStatus.Signed);
        }

        /// <summary>
        /// Kiểm tra signature đã kết thúc hay chưa.
        /// </summary>
        public static bool IsTerminal(SignatureStatus status)
        {
            return status is
                SignatureStatus.Signed or
                SignatureStatus.Declined or
                SignatureStatus.Invalidated;
        }
    }
}