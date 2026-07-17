namespace ContractManagement.API.Common.Enums
{
    /// <summary>
    /// Trạng thái vòng đời pháp lý/thương mại của hợp đồng.
    ///
    /// Lưu ý:
    /// - Chỉ quản lý vòng đời chính của hợp đồng.
    /// - Chữ ký, bản cứng, triển khai, thanh toán và chứng từ
    ///   sẽ có trạng thái độc lập.
    /// </summary>
    public enum ContractStatus : byte
    {
        /// <summary>
        /// Hợp đồng mới tạo, chưa gửi khách hàng.
        /// </summary>
        Draft = 0,

        /// <summary>
        /// Hợp đồng đang được hai bên trao đổi và đàm phán.
        /// </summary>
        Negotiating = 1,

        /// <summary>
        /// Nội dung đã được chốt tạm thời và đang chờ duyệt nội bộ.
        /// Hợp đồng bị khóa chỉnh sửa.
        /// </summary>
        PendingApproval = 2,

        /// <summary>
        /// Hợp đồng đã được duyệt và đang chờ các bên ký.
        /// Chi tiết bên nào đã ký sẽ nằm trong Signature workflow.
        /// </summary>
        PendingSignature = 3,

        /// <summary>
        /// Hai bên đã ký cùng một phiên bản hợp đồng.
        /// Hợp đồng gốc từ đây trở đi không được sửa trực tiếp.
        /// </summary>
        Signed = 4,

        /// <summary>
        /// Hợp đồng đã vượt qua toàn bộ completion gate:
        /// - ký đủ hai bên;
        /// - bản cứng đã lưu;
        /// - triển khai đã nghiệm thu;
        /// - thanh toán đủ;
        /// - đủ chứng từ bắt buộc.
        /// </summary>
        Completed = 5,

        /// <summary>
        /// Owner/Admin chủ động dừng quy trình trước khi ký.
        ///
        /// Ví dụ:
        /// - tạo nhầm hoặc trùng hợp đồng;
        /// - khách hàng rút lui;
        /// - hai bên không tiếp tục giao dịch.
        /// </summary>
        Cancelled = 6,

        /// <summary>
        /// Hợp đồng bị người có thẩm quyền từ chối dứt điểm
        /// tại bước xét duyệt.
        ///
        /// Nếu chỉ yêu cầu sửa lại thì dùng action Returned
        /// để đưa hợp đồng về Negotiating.
        /// </summary>
        Rejected = 7
    }
}