using ContractManagement.API.Common.Enums;

namespace ContractManagement.API.Domains.Policies.Contract
{
    /// <summary>
    /// Quản lý yêu cầu override cho phép triển khai sớm.
    ///
    /// Quy tắc:
    /// - yêu cầu phải có lý do;
    /// - chỉ Manager/Sếp được duyệt hoặc từ chối;
    /// - override có thể bị thu hồi hoặc hết hạn;
    /// - mọi thao tác sau này phải được ghi audit trail.
    /// </summary>
    public static class DeploymentOverridePolicy
    {
        private static readonly IReadOnlyDictionary<
            DeploymentOverrideStatus,
            HashSet<DeploymentOverrideStatus>> AllowedTransitions =
            new Dictionary<
                DeploymentOverrideStatus,
                HashSet<DeploymentOverrideStatus>>
            {
                [DeploymentOverrideStatus.Pending] = new()
                {
                    DeploymentOverrideStatus.Approved,
                    DeploymentOverrideStatus.Rejected
                },

                [DeploymentOverrideStatus.Approved] = new()
                {
                    DeploymentOverrideStatus.Revoked,
                    DeploymentOverrideStatus.Expired
                },

                [DeploymentOverrideStatus.Rejected] = new(),
                [DeploymentOverrideStatus.Revoked] = new(),
                [DeploymentOverrideStatus.Expired] = new()
            };

        public static bool CanTransition(
            DeploymentOverrideStatus currentStatus,
            DeploymentOverrideStatus targetStatus)
        {
            return AllowedTransitions.TryGetValue(
                       currentStatus,
                       out var allowedTargets)
                   && allowedTargets.Contains(targetStatus);
        }

        public static void EnsureCanTransition(
            DeploymentOverrideStatus currentStatus,
            DeploymentOverrideStatus targetStatus)
        {
            if (CanTransition(currentStatus, targetStatus))
            {
                return;
            }

            throw new InvalidOperationException(
                $"Không thể chuyển override từ " +
                $"{currentStatus} sang {targetStatus}.");
        }

        /// <summary>
        /// Kiểm tra dữ liệu khi nhân viên tạo yêu cầu override.
        /// </summary>
        public static void EnsureCanCreateRequest(string? reason)
        {
            EnsureReasonProvided(reason);
        }

        /// <summary>
        /// Kiểm tra quyền và dữ liệu khi Sếp duyệt/từ chối override.
        /// </summary>
        public static void EnsureCanDecide(
            DeploymentOverrideStatus currentStatus,
            DeploymentOverrideStatus targetStatus,
            EmployeeType approverType,
            string? reason)
        {
            if (approverType != EmployeeType.Manager)
            {
                throw new UnauthorizedAccessException(
                    "Chỉ Sếp mới có quyền duyệt hoặc từ chối " +
                    "yêu cầu triển khai ngoại lệ.");
            }

            if (targetStatus is not
                DeploymentOverrideStatus.Approved and not
                DeploymentOverrideStatus.Rejected)
            {
                throw new InvalidOperationException(
                    "Kết quả xét duyệt override chỉ có thể là " +
                    "Approved hoặc Rejected.");
            }

            EnsureReasonProvided(reason);

            EnsureCanTransition(
                currentStatus,
                targetStatus);
        }

        /// <summary>
        /// Chỉ Sếp được thu hồi một override đã duyệt.
        /// </summary>
        public static void EnsureCanRevoke(
            DeploymentOverrideStatus currentStatus,
            EmployeeType actorType,
            string? reason)
        {
            if (actorType != EmployeeType.Manager)
            {
                throw new UnauthorizedAccessException(
                    "Chỉ Sếp mới có quyền thu hồi override.");
            }

            EnsureReasonProvided(reason);

            EnsureCanTransition(
                currentStatus,
                DeploymentOverrideStatus.Revoked);
        }

        /// <summary>
        /// Kiểm tra override có đang hiệu lực hay không.
        ///
        /// utcNow được truyền từ ngoài vào để unit test ổn định,
        /// thay vì gọi DateTime.UtcNow trực tiếp bên trong policy.
        /// </summary>
        public static bool IsActive(
            DeploymentOverrideStatus status,
            DateTime? expiresAt,
            DateTime utcNow)
        {
            if (status != DeploymentOverrideStatus.Approved)
            {
                return false;
            }

            // Không có ngày hết hạn nghĩa là override vẫn còn hiệu lực.
            if (!expiresAt.HasValue)
            {
                return true;
            }

            return expiresAt.Value > utcNow;
        }

        private static void EnsureReasonProvided(string? reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                throw new ArgumentException(
                    "Yêu cầu triển khai ngoại lệ phải có lý do.");
            }
        }
    }
}