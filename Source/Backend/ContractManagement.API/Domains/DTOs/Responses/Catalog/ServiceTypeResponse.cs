namespace ContractManagement.Domains.DTOs.Responses.Catalog
{
    /// <summary>
    /// Response loại dịch vụ.
    /// ServiceCount cho biết loại dịch vụ này đang có bao nhiêu service sử dụng.
    /// </summary>
    public class ServiceTypeResponse
    {
        public byte ServiceTypeId { get; set; }

        public string? ServiceTypeName { get; set; }

        public byte? LangId { get; set; }

        public int ServiceCount { get; set; }
    }
}