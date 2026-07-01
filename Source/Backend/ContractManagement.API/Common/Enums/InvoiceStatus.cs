namespace ContractManagement.Common.Enums
{
    /// <summary>
    /// Trạng thái hóa đơn.
    /// </summary>
    public enum InvoiceStatus : byte
    {
        Unpaid = 0,
        PartialPaid = 1,
        Paid = 2
    }
}