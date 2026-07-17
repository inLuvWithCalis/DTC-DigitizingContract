namespace ContractManagement.API.Common.Enums
{
    /// <summary>
    /// Trạng thái của một yêu cầu xét duyệt hợp đồng.
    ///
    /// Một hợp đồng có thể có nhiều approval request.
    /// Ví dụ: request đầu bị Returned, nhân viên sửa hợp đồng
    /// rồi tạo một request mới để gửi duyệt lại.
    /// </summary>
    public enum ApprovalRequestStatus : byte
    {
        /// <summary>
        /// Đã gửi yêu cầu và đang chờ người có thẩm quyền xử lý.
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Yêu cầu đã được duyệt.
        /// Hợp đồng có thể chuyển sang PendingSignature.
        /// </summary>
        Approved = 1,

        /// <summary>
        /// Người duyệt yêu cầu chỉnh sửa lại hợp đồng.
        /// Hợp đồng quay về Negotiating.
        /// </summary>
        Returned = 2,

        /// <summary>
        /// Người duyệt từ chối dứt điểm.
        /// Hợp đồng chuyển sang Rejected.
        /// </summary>
        Rejected = 3,

        /// <summary>
        /// Người gửi chủ động rút yêu cầu trước khi có quyết định.
        ///
        /// Việc này chỉ rút approval request,
        /// không có nghĩa là hủy toàn bộ hợp đồng.
        /// Hợp đồng quay về Negotiating.
        /// </summary>
        Withdrawn = 4
    }
}