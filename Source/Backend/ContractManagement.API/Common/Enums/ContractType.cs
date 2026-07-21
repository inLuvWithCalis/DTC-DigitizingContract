namespace ContractManagement.API.Common.Enums
{
    /// <summary>
    /// Loại hợp đồng pháp lý.
    ///
    /// Biên bản, báo giá và công văn thanh toán không nằm ở đây
    /// chúng là supporting documents, không phải Contract chính.
    /// </summary>
    public enum ContractType : byte
    {
        SoftwareSupply = 1,
        SoftwareMaintenance = 2,
        SoftwareUpkeep = 3
    }
}