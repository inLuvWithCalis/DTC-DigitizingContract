namespace ContractManagement.Common.Enums
{
    /// <summary>
    /// Trạng thái bản cứng của hợp đồng.
    /// </summary>
    public enum HardCopyStatus : byte
    {
        NotSent = 0,
        SentToCustomer = 1,
        ReceivedBack = 2
    }
}