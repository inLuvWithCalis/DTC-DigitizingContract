namespace ContractManagement.API.Common.Enums
{
    /// <summary>
    /// Trạng thái của yêu cầu cho phép triển khai
    /// trước khi hoàn tất việc thu hồi bản cứng.
    /// </summary>
    public enum DeploymentOverrideStatus : byte
    {
        /// <summary>
        /// Đang chờ Sếp xem xét.
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Đã được Sếp phê duyệt.
        /// Override có thể được dùng để mở khóa triển khai.
        /// </summary>
        Approved = 1,

        /// <summary>
        /// Sếp từ chối yêu cầu.
        /// </summary>
        Rejected = 2,

        /// <summary>
        /// Override đã được Sếp thu hồi.
        /// </summary>
        Revoked = 3,

        /// <summary>
        /// Override đã hết thời hạn hiệu lực.
        /// </summary>
        Expired = 4
    }
}