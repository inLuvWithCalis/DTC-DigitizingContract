namespace ContractManagement.API.Common.Enums
{
    /// <summary>
    /// Trạng thái xử lý bản cứng của hợp đồng hoặc phụ lục.
    ///
    /// Luồng bản cứng độc lập với ContractStatus.
    /// Boss override triển khai không làm thay đổi trạng thái này.
    /// </summary>
    public enum HardCopyStatus : byte
    {
        /// <summary>
        /// Chưa chuẩn bị bản cứng.
        /// </summary>
        NotPrepared = 0,

        /// <summary>
        /// Đã in và chuẩn bị bản cứng để gửi khách hàng.
        /// </summary>
        Prepared = 1,

        /// <summary>
        /// Bản cứng đã được gửi cho khách hàng.
        /// </summary>
        SentToCustomer = 2,

        /// <summary>
        /// Khách hàng đã xác nhận nhận được bản cứng.
        /// </summary>
        CustomerReceived = 3,

        /// <summary>
        /// Công ty đã nhận lại bản cứng có đầy đủ
        /// chữ ký và dấu của khách hàng.
        /// </summary>
        ReturnedSignedToCompany = 4,

        /// <summary>
        /// Bản cứng đã được Admin Officer lưu vào kho công ty.
        ///
        /// Đây là trạng thái đạt hard-copy completion gate.
        /// </summary>
        Stored = 5
    }
}