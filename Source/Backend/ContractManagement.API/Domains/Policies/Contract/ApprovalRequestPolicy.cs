using ContractManagement.API.Common.Enums;

namespace ContractManagement.API.Domains.Policies.Contract
{
    /// <summary>
    /// Quản lý vòng đời của một yêu cầu xét duyệt hợp đồng.
    ///
    /// ApprovalRequestPolicy khác ContractLifecyclePolicy:
    ///
    /// - ContractLifecyclePolicy quản lý trạng thái hợp đồng.
    /// - ApprovalRequestPolicy quản lý trạng thái của từng lần gửi duyệt.
    /// </summary>
    public static class ApprovalRequestPolicy
    {
        /// <summary>
        /// Mỗi approval request bắt đầu ở Pending
        /// và chỉ được xử lý đúng một lần.
        ///
        /// Sau khi Approved, Returned, Rejected hoặc Withdrawn,
        /// request đó trở thành terminal và không được thay đổi lại.
        /// </summary>
        private static readonly IReadOnlyDictionary<
            ApprovalRequestStatus,
            HashSet<ApprovalRequestStatus>> AllowedTransitions =
            new Dictionary<
                ApprovalRequestStatus,
                HashSet<ApprovalRequestStatus>>
            {
                [ApprovalRequestStatus.Pending] = new()
                {
                    ApprovalRequestStatus.Approved,
                    ApprovalRequestStatus.Returned,
                    ApprovalRequestStatus.Rejected,
                    ApprovalRequestStatus.Withdrawn
                },

                // Các trạng thái đã có kết quả là terminal.
                [ApprovalRequestStatus.Approved] = new(),
                [ApprovalRequestStatus.Returned] = new(),
                [ApprovalRequestStatus.Rejected] = new(),
                [ApprovalRequestStatus.Withdrawn] = new()
            };

        /// <summary>
        /// Kiểm tra approval request có được chuyển trạng thái hay không.
        /// </summary>
        public static bool CanTransition(
            ApprovalRequestStatus currentStatus,
            ApprovalRequestStatus targetStatus)
        {
            return AllowedTransitions.TryGetValue(
                       currentStatus,
                       out var allowedTargets)
                   && allowedTargets.Contains(targetStatus);
        }

        /// <summary>
        /// Chặn khi một approval request bị xử lý sai vòng đời.
        /// </summary>
        public static void EnsureCanTransition(
            ApprovalRequestStatus currentStatus,
            ApprovalRequestStatus targetStatus)
        {
            if (CanTransition(currentStatus, targetStatus))
            {
                return;
            }

            throw new InvalidOperationException(
                $"Không thể chuyển yêu cầu duyệt từ " +
                $"{currentStatus} sang {targetStatus}.");
        }

        /// <summary>
        /// Kiểm tra approval request đã kết thúc hay chưa.
        ///
        /// Khi đã terminal, muốn gửi duyệt lại phải tạo request mới,
        /// không được tái sử dụng request cũ.
        /// </summary>
        public static bool IsTerminal(ApprovalRequestStatus status)
        {
            return status is
                ApprovalRequestStatus.Approved or
                ApprovalRequestStatus.Returned or
                ApprovalRequestStatus.Rejected or
                ApprovalRequestStatus.Withdrawn;
        }

        /// <summary>
        /// Xác định trạng thái hợp đồng sau khi approval request
        /// nhận được kết quả.
        /// </summary>
        public static ContractStatus GetTargetContractStatus(
            ApprovalRequestStatus approvalResult)
        {
            return approvalResult switch
            {
                /*
                 * Sếp duyệt hợp đồng:
                 * chuyển sang giai đoạn chờ các bên ký.
                 */
                ApprovalRequestStatus.Approved =>
                    ContractStatus.PendingSignature,

                /*
                 * Sếp yêu cầu chỉnh sửa:
                 * hợp đồng quay lại giai đoạn đàm phán.
                 */
                ApprovalRequestStatus.Returned =>
                    ContractStatus.Negotiating,

                /*
                 * Sếp từ chối dứt điểm:
                 * hợp đồng chuyển sang trạng thái Rejected.
                 */
                ApprovalRequestStatus.Rejected =>
                    ContractStatus.Rejected,

                /*
                 * Người gửi rút request:
                 * hợp đồng vẫn tồn tại và quay về Negotiating.
                 */
                ApprovalRequestStatus.Withdrawn =>
                    ContractStatus.Negotiating,

                /*
                 * Pending chưa phải kết quả xét duyệt,
                 * nên không thể xác định trạng thái hợp đồng tiếp theo.
                 */
                _ => throw new InvalidOperationException(
                    "Approval request chưa có kết quả cuối cùng.")
            };
        }

        /// <summary>
        /// Kiểm tra đồng thời:
        ///
        /// 1. Approval request được phép nhận kết quả này.
        /// 2. Hợp đồng được phép chuyển sang trạng thái tương ứng.
        ///
        /// Hàm này giúp Application Service không bỏ sót
        /// một trong hai state machine.
        /// </summary>
        public static void EnsureCanApplyResult(
            ApprovalRequestStatus currentApprovalStatus,
            ApprovalRequestStatus approvalResult,
            ContractStatus currentContractStatus)
        {
            // Kiểm tra vòng đời approval request.
            EnsureCanTransition(
                currentApprovalStatus,
                approvalResult);

            // Lấy trạng thái hợp đồng tương ứng với kết quả duyệt.
            var targetContractStatus =
                GetTargetContractStatus(approvalResult);

            // Kiểm tra vòng đời hợp đồng.
            ContractLifecyclePolicy.EnsureCanTransition(
                currentContractStatus,
                targetContractStatus);
        }
    }
}