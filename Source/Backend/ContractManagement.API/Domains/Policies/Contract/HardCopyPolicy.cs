using ContractManagement.API.Common.Enums;

namespace ContractManagement.API.Domains.Policies.Contract
{
    /// <summary>
    /// Quản lý vòng đời bản cứng của hợp đồng hoặc phụ lục.
    ///
    /// Bản cứng là một workflow độc lập.
    /// Việc Sếp cho phép triển khai sớm không được tự động
    /// thay đổi trạng thái bản cứng.
    /// </summary>
    public static class HardCopyPolicy
    {
        private static readonly IReadOnlyDictionary<
            HardCopyStatus,
            HashSet<HardCopyStatus>> AllowedTransitions =
            new Dictionary<HardCopyStatus, HashSet<HardCopyStatus>>
            {
                [HardCopyStatus.NotPrepared] = new()
                {
                    HardCopyStatus.Prepared
                },

                [HardCopyStatus.Prepared] = new()
                {
                    HardCopyStatus.SentToCustomer
                },

                [HardCopyStatus.SentToCustomer] = new()
                {
                    HardCopyStatus.CustomerReceived
                },

                [HardCopyStatus.CustomerReceived] = new()
                {
                    HardCopyStatus.ReturnedSignedToCompany
                },

                [HardCopyStatus.ReturnedSignedToCompany] = new()
                {
                    HardCopyStatus.Stored
                },

                // Bản cứng đã lưu kho là terminal state.
                [HardCopyStatus.Stored] = new()
            };

        /// <summary>
        /// Kiểm tra bước chuyển trạng thái bản cứng.
        /// </summary>
        public static bool CanTransition(
            HardCopyStatus currentStatus,
            HardCopyStatus targetStatus)
        {
            return AllowedTransitions.TryGetValue(
                       currentStatus,
                       out var allowedTargets)
                   && allowedTargets.Contains(targetStatus);
        }

        /// <summary>
        /// Chặn việc bỏ qua các bước xử lý bản cứng.
        /// </summary>
        public static void EnsureCanTransition(
            HardCopyStatus currentStatus,
            HardCopyStatus targetStatus)
        {
            if (CanTransition(currentStatus, targetStatus))
            {
                return;
            }

            throw new InvalidOperationException(
                $"Không thể chuyển trạng thái bản cứng từ " +
                $"{currentStatus} sang {targetStatus}.");
        }

        /// <summary>
        /// Kiểm tra bản cứng đã hoàn tất việc lưu kho hay chưa.
        /// </summary>
        public static bool IsStored(HardCopyStatus status)
        {
            return status == HardCopyStatus.Stored;
        }

        /// <summary>
        /// Stored là trạng thái kết thúc của luồng bản cứng.
        /// </summary>
        public static bool IsTerminal(HardCopyStatus status)
        {
            return status == HardCopyStatus.Stored;
        }
    }
}