namespace ContractManagement.API.Common.Enums
{
    /// <summary>
    /// Phương thức ký hợp đồng hoặc phụ lục.
    /// </summary>
    public enum SignatureMethod : byte
    {
        /// <summary>
        /// Ký online trên hệ thống và xác thực bằng SMS OTP.
        /// </summary>
        OtpElectronic = 0,

        /// <summary>
        /// Ký tay trên giấy, sau đó scan và upload.
        /// Phương thức này không yêu cầu OTP.
        /// </summary>
        WetInkScan = 1
    }
}