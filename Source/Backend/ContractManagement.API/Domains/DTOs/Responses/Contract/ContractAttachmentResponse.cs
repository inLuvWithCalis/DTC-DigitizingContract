namespace ContractManagement.Domains.DTOs.Responses.Contract
{
    /// <summary>
    /// Response trả về file đính kèm của hợp đồng.
    /// </summary>
    public class ContractAttachmentResponse
    {
        public int AttachmentId { get; set; }

        public int ContractId { get; set; }

        public string? ContractFileName { get; set; }

        public string? ContractFilePath { get; set; }

        public byte DocumentType { get; set; }

        public string DocumentTypeName { get; set; } = string.Empty;

        public DateTime? UploadDate { get; set; }

        public int? UploadEmployeeId { get; set; }
    }
}