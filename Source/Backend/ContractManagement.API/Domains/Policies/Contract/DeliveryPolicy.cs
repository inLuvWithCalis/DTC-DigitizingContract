using ContractManagement.API.Common.Enums;

namespace ContractManagement.API.Domains.Policies.Contract
{
    /// <summary>
    /// Quản lý vòng đời triển khai kỹ thuật và deployment gate.
    ///
    /// Technical Staff không được tự ý bắt đầu triển khai.
    /// Service phải gọi policy này trước khi chuyển DeliveryStatus
    /// từ Pending sang InProgress.
    /// </summary>
    public static class DeliveryPolicy
    {
        private static readonly IReadOnlyDictionary<
            DeliveryStatus,
            HashSet<DeliveryStatus>> AllowedTransitions =
            new Dictionary<DeliveryStatus, HashSet<DeliveryStatus>>
            {
                [DeliveryStatus.Pending] = new()
                {
                    DeliveryStatus.InProgress
                },

                [DeliveryStatus.InProgress] = new()
                {
                    DeliveryStatus.Accepted
                },

                // Accepted là trạng thái hoàn thành triển khai.
                [DeliveryStatus.Accepted] = new()
            };

        public static bool CanTransition(
            DeliveryStatus currentStatus,
            DeliveryStatus targetStatus)
        {
            return AllowedTransitions.TryGetValue(
                       currentStatus,
                       out var allowedTargets)
                   && allowedTargets.Contains(targetStatus);
        }

        public static void EnsureCanTransition(
            DeliveryStatus currentStatus,
            DeliveryStatus targetStatus)
        {
            if (CanTransition(currentStatus, targetStatus))
            {
                return;
            }

            throw new InvalidOperationException(
                $"Không thể chuyển trạng thái triển khai từ " +
                $"{currentStatus} sang {targetStatus}.");
        }

        /// <summary>
        /// Kiểm tra các điều kiện bắt buộc trước khi triển khai.
        /// </summary>
        public static bool CanStartDeployment(
            ContractStatus contractStatus,
            DeliveryStatus deliveryStatus,
            HardCopyStatus hardCopyStatus,
            bool hasTechnicalAssignment,
            bool hasActiveBossOverride)
        {
            return contractStatus == ContractStatus.Signed
                   && deliveryStatus == DeliveryStatus.Pending
                   && hasTechnicalAssignment
                   && (
                       hardCopyStatus == HardCopyStatus.Stored
                       || hasActiveBossOverride
                   );
        }

        /// <summary>
        /// Trả lỗi cụ thể khi chưa đủ điều kiện triển khai.
        /// </summary>
        public static void EnsureCanStartDeployment(
            ContractStatus contractStatus,
            DeliveryStatus deliveryStatus,
            HardCopyStatus hardCopyStatus,
            bool hasTechnicalAssignment,
            bool hasActiveBossOverride)
        {
            if (contractStatus != ContractStatus.Signed)
            {
                throw new InvalidOperationException(
                    "Chỉ hợp đồng đã ký đủ hai bên mới được triển khai.");
            }

            if (deliveryStatus != DeliveryStatus.Pending)
            {
                throw new InvalidOperationException(
                    "Chỉ delivery đang Pending mới được bắt đầu triển khai.");
            }

            if (!hasTechnicalAssignment)
            {
                throw new InvalidOperationException(
                    "Hợp đồng chưa được phân công cho nhân viên kỹ thuật.");
            }

            var hardCopyStored =
                HardCopyPolicy.IsStored(hardCopyStatus);

            if (!hardCopyStored && !hasActiveBossOverride)
            {
                throw new InvalidOperationException(
                    "Chưa nhận và lưu bản cứng hợp đồng. " +
                    "Cần bản cứng đã Stored hoặc override được Sếp duyệt.");
            }

            EnsureCanTransition(
                deliveryStatus,
                DeliveryStatus.InProgress);
        }

        /// <summary>
        /// Chỉ được đánh dấu Accepted khi:
        /// - triển khai đang InProgress;
        /// - đã có biên bản nghiệm thu số hóa.
        /// </summary>
        public static void EnsureCanAccept(
            DeliveryStatus currentStatus,
            bool hasAcceptanceRecord)
        {
            EnsureCanTransition(
                currentStatus,
                DeliveryStatus.Accepted);

            if (!hasAcceptanceRecord)
            {
                throw new InvalidOperationException(
                    "Không thể hoàn tất triển khai khi chưa có " +
                    "biên bản nghiệm thu số hóa.");
            }
        }

        public static bool IsTerminal(DeliveryStatus status)
        {
            return status == DeliveryStatus.Accepted;
        }
    }
}