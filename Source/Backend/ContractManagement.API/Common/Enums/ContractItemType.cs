namespace ContractManagement.API.Common.Enums
{
    /// <summary>
    /// Loại sản phẩm hoặc dịch vụ được đưa vào hợp đồng.
    ///
    /// Giá trị phải khớp với cột ItemType và check constraint
    /// trong bảng tbl_ContractItem.
    /// </summary>
    public enum ContractItemType : byte
    {
        Product = 1,
        Service = 2
    }
}