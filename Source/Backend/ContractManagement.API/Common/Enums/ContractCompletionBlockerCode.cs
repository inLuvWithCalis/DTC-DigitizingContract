namespace ContractManagement.API.Common.Enums
{
    /// <summary>
    /// Mã nguyên nhân khiến hợp đồng chưa đủ điều kiện chuyển sang Completed.
    ///
    /// Enum này chỉ phục vụ kết quả kiểm tra nghiệp vụ,
    /// không được lưu trực tiếp vào database.
    /// </summary>
    public enum ContractCompletionBlockerCode
    {
        /// <summary>
        /// Hợp đồng chưa hoàn tất quy trình ký.
        /// </summary>
        ContractMustBeSigned = 1,

        /// <summary>
        /// Đại diện DTC chưa ký hợp lệ.
        /// </summary>
        ProviderSignatureMustBeSigned = 2,

        /// <summary>
        /// Đại diện khách hàng chưa ký hợp lệ.
        /// </summary>
        CustomerSignatureMustBeSigned = 3,

        /// <summary>
        /// Bản cứng có đầy đủ chữ ký, con dấu chưa được thu hồi và lưu kho.
        /// </summary>
        HardCopyMustBeStored = 4,

        /// <summary>
        /// Quy trình triển khai chưa được nghiệm thu.
        /// </summary>
        DeliveryMustBeAccepted = 5,

        /// <summary>
        /// Khách hàng chưa thanh toán đủ số tiền phải thanh toán.
        /// </summary>
        PaymentMustBeFullyPaid = 6,

        /// <summary>
        /// Thiếu một chứng từ bắt buộc.
        /// Tên chứng từ được đặt trong Reference của blocker.
        /// </summary>
        RequiredDocumentMissing = 7
    }
}