namespace ContractManagement.Common.Enums
{
    /// <summary>
    /// Trạng thái thanh toán.
    /// </summary>
    public enum PaymentStatus : byte
    {
        Pending = 0,
        Paid = 1,
        Overdue = 2
    }
}