using ContractManagement.API.Common.Enums;

namespace ContractManagement.API.Domains.Policies.Contract
{
    /// <summary>
    /// Quản lý quy tắc chuyển trạng thái và stage-gate của hợp đồng.
    ///
    /// Đây là domain policy thuần:
    /// - không truy cập database;
    /// - không phụ thuộc HTTP;
    /// - không cần đăng ký Dependency Injection;
    /// - có thể unit test độc lập.
    /// </summary>
    public static class ContractLifecyclePolicy
    {
        /// <summary>
        /// Danh sách các bước chuyển trạng thái được phép.
        /// </summary>
        private static readonly IReadOnlyDictionary<
            ContractStatus,
            HashSet<ContractStatus>> AllowedTransitions =
            new Dictionary<ContractStatus, HashSet<ContractStatus>>
            {
                [ContractStatus.Draft] = new()
                {
                    // Bắt đầu gửi khách hàng xem và đàm phán.
                    ContractStatus.Negotiating,

                    // Hủy bản nháp do tạo nhầm, trùng hoặc không còn nhu cầu.
                    ContractStatus.Cancelled
                },

                [ContractStatus.Negotiating] = new()
                {
                    // Hai bên đã chốt nội dung, gửi duyệt nội bộ.
                    ContractStatus.PendingApproval,

                    // Khách hàng rút lui hoặc hai bên dừng giao dịch.
                    ContractStatus.Cancelled
                },

                [ContractStatus.PendingApproval] = new()
                {
                    /*
                     * Approver trả lại để chỉnh sửa.
                     * Đây là action Returned, không phải Rejected.
                     */
                    ContractStatus.Negotiating,

                    // Approver duyệt, chuyển sang luồng ký.
                    ContractStatus.PendingSignature,

                    // Approver từ chối dứt điểm.
                    ContractStatus.Rejected,

                    // Owner/Admin rút hồ sơ trong lúc chờ duyệt.
                    ContractStatus.Cancelled
                },

                [ContractStatus.PendingSignature] = new()
                {
                    /*
                     * Nội dung cần thay đổi trong lúc ký:
                     * - hủy hiệu lực approval/signature đang chờ;
                     * - quay lại Negotiating;
                     * - sửa nội dung và tạo version mới.
                     */
                    ContractStatus.Negotiating,

                    /*
                     * Chỉ chuyển sang Signed khi Signature workflow
                     * xác nhận cả hai bên đã ký cùng một ContractVersion.
                     */
                    ContractStatus.Signed,

                    /*
                     * Dừng quy trình khi chưa ký đủ hai bên.
                     * Ví dụ khách hàng từ chối tiếp tục ký.
                     */
                    ContractStatus.Cancelled
                },

                [ContractStatus.Signed] = new()
                {
                    /*
                     * Chỉ chuyển sang Completed khi CompletionEvaluator
                     * xác nhận tất cả điều kiện đã đạt:
                     *
                     * - ký đủ hai bên;
                     * - bản cứng đã lưu;
                     * - triển khai Accepted;
                     * - thanh toán FullyPaid;
                     * - đủ chứng từ bắt buộc.
                     */
                    ContractStatus.Completed
                },

                /*
                 * Completed, Cancelled và Rejected là terminal state,
                 * không được chuyển tiếp sang trạng thái khác.
                 */
                [ContractStatus.Completed] = new(),
                [ContractStatus.Cancelled] = new(),
                [ContractStatus.Rejected] = new()
            };

        /// <summary>
        /// Kiểm tra đường chuyển trạng thái có hợp lệ hay không.
        ///
        /// Hàm này chỉ kiểm tra state machine.
        /// Những điều kiện như chữ ký, thanh toán và bản cứng
        /// sẽ được kiểm tra bởi các policy/evaluator riêng.
        /// </summary>
        public static bool CanTransition(
            ContractStatus currentStatus,
            ContractStatus targetStatus)
        {
            return AllowedTransitions.TryGetValue(
                       currentStatus,
                       out var allowedTargets)
                   && allowedTargets.Contains(targetStatus);
        }

        /// <summary>
        /// Ném exception nếu service cố chuyển trạng thái sai.
        /// </summary>
        public static void EnsureCanTransition(
            ContractStatus currentStatus,
            ContractStatus targetStatus)
        {
            if (CanTransition(currentStatus, targetStatus))
            {
                return;
            }

            throw new InvalidOperationException(
                $"Không thể chuyển hợp đồng từ " +
                $"{currentStatus} sang {targetStatus}.");
        }

        /// <summary>
        /// Chỉ Draft và Negotiating được sửa nội dung.
        /// </summary>
        public static bool CanEditContent(ContractStatus status)
        {
            return status is
                ContractStatus.Draft or
                ContractStatus.Negotiating;
        }

        /// <summary>
        /// Kiểm tra nội dung hợp đồng đã bị khóa hay chưa.
        /// </summary>
        public static bool IsContentLocked(ContractStatus status)
        {
            return !CanEditContent(status);
        }

        /// <summary>
        /// Chỉ được tạo version mới trong giai đoạn còn cho phép sửa.
        /// </summary>
        public static bool CanCreateVersion(ContractStatus status)
        {
            return status is
                ContractStatus.Draft or
                ContractStatus.Negotiating;
        }

        /// <summary>
        /// Kiểm tra trạng thái kết thúc.
        /// </summary>
        public static bool IsTerminal(ContractStatus status)
        {
            return status is
                ContractStatus.Completed or
                ContractStatus.Cancelled or
                ContractStatus.Rejected;
        }
    }
}