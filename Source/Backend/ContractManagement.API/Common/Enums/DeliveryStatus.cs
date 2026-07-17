namespace ContractManagement.API.Common.Enums
{
    /// <summary>
    /// Trạng thái triển khai kỹ thuật của hợp đồng.
    ///
    /// Trạng thái này độc lập với ContractStatus.
    /// </summary>
    public enum DeliveryStatus : byte
    {
        /// <summary>
        /// Chờ đủ điều kiện và chờ đội kỹ thuật triển khai.
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Đội kỹ thuật đang triển khai cho khách hàng.
        /// </summary>
        InProgress = 1,

        /// <summary>
        /// Công việc kỹ thuật đã hoàn thành và được nghiệm thu.
        /// </summary>
        Accepted = 2
    }
}