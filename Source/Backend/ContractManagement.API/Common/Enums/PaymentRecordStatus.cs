namespace ContractManagement.API.Common.Enums
{
    /// <summary>
    /// Trạng thái của từng khoản thanh toán do kế toán nhập.
    ///
    /// Chỉ khoản thanh toán Confirmed mới được cộng vào
    /// tổng số tiền khách hàng đã thanh toán.
    /// </summary>
    public enum PaymentRecordStatus : byte
    {
        /// <summary>
        /// Khoản thanh toán vừa được nhập, chưa xác nhận.
        /// Chưa được cộng vào tổng tiền đã nhận.
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Khoản thanh toán đã được xác nhận hợp lệ.
        /// Được cộng vào tổng tiền đã nhận.
        /// </summary>
        Confirmed = 1,

        /// <summary>
        /// Bản ghi bị vô hiệu do nhập sai hoặc bị hủy.
        /// Không được xóa vật lý và không được tính vào tổng tiền.
        /// </summary>
        Voided = 2
    }
}