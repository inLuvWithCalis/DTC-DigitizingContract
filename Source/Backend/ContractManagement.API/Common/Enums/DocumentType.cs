namespace ContractManagement.Common.Enums
{
    /// <summary>
    /// Loại tài liệu/file đính kèm trong hệ thống.
    /// Dùng cho FileStorage hoặc ContractAttachment.
    /// </summary>
    public enum DocumentType : byte
    {
        QuotationFile = 0,
        AcceptanceRecord = 1,
        HandoverRecord = 2,
        LiquidationRecord = 3,
        VATInvoice = 4,
        BankGuarantee = 5,
        SignedScanCopy = 6,
        Other = 99
    }
}