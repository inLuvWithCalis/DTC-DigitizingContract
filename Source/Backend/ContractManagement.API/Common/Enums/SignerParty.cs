namespace ContractManagement.API.Common.Enums
{
    /// <summary>
    /// Xác định bên tham gia ký hợp đồng.
    /// </summary>
    public enum SignerParty : byte
    {
        /// <summary>
        /// Khách hàng — Bên A.
        /// </summary>
        Customer = 0,

        /// <summary>
        /// Nhà cung cấp/DTC — Bên B.
        /// </summary>
        Provider = 1
    }
}