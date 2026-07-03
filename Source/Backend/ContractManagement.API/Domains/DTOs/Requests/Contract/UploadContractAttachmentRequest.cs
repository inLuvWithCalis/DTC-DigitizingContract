using System.ComponentModel.DataAnnotations;

namespace ContractManagement.Domains.DTOs.Requests.Contract
{
    /// <summary>
    /// Request upload file đính kèm riêng cho hợp đồng.
    /// </summary>
    public class UploadContractAttachmentRequest
    {
        [Required]
        public IFormFile File { get; set; } = default!;

        /// <summary>
        /// Loại chứng từ.
        /// Ví dụ:
        /// 0 = QuotationFile
        /// 1 = AcceptanceRecord
        /// 2 = HandoverRecord
        /// 3 = LiquidationRecord
        /// 4 = VATInvoice
        /// 5 = BankGuarantee
        /// 6 = SignedScanCopy
        /// 99 = Other
        /// </summary>
        [Range(0, 99)]
        public byte DocumentType { get; set; }
    }
}