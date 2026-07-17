namespace ContractManagement.API.Common.Enums
{
    /// <summary>
    /// Trạng thái của một yêu cầu/lượt ký cụ thể.
    ///
    /// Mỗi bên ký sẽ có signature record riêng.
    /// </summary>
    public enum SignatureStatus : byte
    {
        /// <summary>
        /// Đang chờ bên tương ứng thực hiện ký.
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Người ký đã hoàn tất ký hợp lệ.
        /// </summary>
        Signed = 1,

        /// <summary>
        /// Người ký từ chối ký.
        /// Contract workflow sẽ quyết định quay lại Negotiating
        /// hay chuyển sang Cancelled.
        /// </summary>
        Declined = 2,

        /// <summary>
        /// Lượt ký không còn hiệu lực.
        ///
        /// Ví dụ: nội dung hợp đồng thay đổi và hệ thống
        /// tạo một ContractVersion mới.
        /// </summary>
        Invalidated = 3
    }
}