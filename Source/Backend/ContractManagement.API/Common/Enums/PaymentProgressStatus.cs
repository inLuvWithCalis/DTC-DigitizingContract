namespace ContractManagement.API.Common.Enums
{
    /// <summary>
    /// Tiến độ thanh toán của hợp đồng hoặc một đợt thanh toán.
    ///
    /// Trạng thái này được hệ thống tính từ các khoản thanh toán
    /// đã được kế toán xác nhận, không được chỉnh bằng checkbox.
    /// </summary>
    public enum PaymentProgressStatus : byte
    {
        /// <summary>
        /// Chưa nhận được khoản tiền hợp lệ nào.
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Đã nhận một phần nhưng chưa đủ số tiền phải thanh toán.
        /// </summary>
        PartiallyPaid = 1,

        /// <summary>
        /// Tổng tiền hợp lệ đã nhận bằng hoặc lớn hơn
        /// tổng số tiền phải thanh toán.
        /// </summary>
        FullyPaid = 2
    }
}