using System.ComponentModel.DataAnnotations;

namespace ContractManagement.API.Domains.DTOs.Requests.Contract
{
    /// <summary>
    /// Item được gửi khi cập nhật Draft.
    ///
    /// Có ContractItemId: cập nhật item hiện có.
    /// Không có ContractItemId: tạo item mới.
    /// </summary>
    public class UpdateContractItemRequest : CreateContractItemRequest
    {
        [Range(1, int.MaxValue)]
        public int? ContractItemId { get; set; }

        /// <summary>
        /// Bắt buộc khi ContractItemId có giá trị.
        /// </summary>
        public string? RowVersion { get; set; }
    }
}