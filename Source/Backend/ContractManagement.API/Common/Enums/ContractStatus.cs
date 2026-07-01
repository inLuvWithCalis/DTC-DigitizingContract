namespace ContractManagement.API.Common.Enums
{
    /// <summary>
    /// Trạng thái vòng đời của hợp đồng.
    /// Lưu ý: Database có thể lưu dạng tinyint/byte.
    /// Khi gán vào entity thì cast: (byte)ContractStatus.Draft
    /// </summary>
    public enum ContractStatus : byte
    {
        Draft = 0,
        Negotiating = 1,
        PendingApproval = 2,
        PendingCustomerSign = 3,
        Signed = 4,
        PendingDocuments = 5,
        PendingPayment = 6,
        Completed = 7,
        Maintain = 8,
        Cancelled = 9
    }
}
