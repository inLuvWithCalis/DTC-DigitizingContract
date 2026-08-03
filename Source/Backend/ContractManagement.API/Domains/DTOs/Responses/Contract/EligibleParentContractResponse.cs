using ContractManagement.API.Common.Enums;

namespace ContractManagement.API.Domains.DTOs.Responses.Contract
{
    /// <summary>
    /// Hợp đồng gốc đủ điều kiện để hiển thị trên dropdown.
    /// </summary>
    public class EligibleParentContractResponse
    {
        public int ContractId { get; set; }

        public string? ContractCode { get; set; }

        public string ContractName { get; set; } = string.Empty;

        public ContractType ContractType { get; set; }

        public ContractStatus Status { get; set; }

        public DateTime? EffectiveDate { get; set; }

        public DateTime? ExpireDate { get; set; }
    }
}