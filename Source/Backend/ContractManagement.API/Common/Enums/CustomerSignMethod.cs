namespace ContractManagement.Common.Enums
{
    /// <summary>
    /// Cách khách hàng ký hợp đồng.
    /// OnlineOTP: ký online bằng OTP.
    /// PrintScan: ký giấy rồi scan/upload.
    /// </summary>
    public enum CustomerSignMethod : byte
    {
        OnlineOTP = 0,
        PrintScan = 1
    }
}