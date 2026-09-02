namespace ContractManagement.Domains.DTOs.Responses.File
{
    /// <summary>
    /// DTO trả về thông tin file đã upload.
    /// Dùng để frontend hiển thị danh sách file hoặc link download.
    /// </summary>
    public class FileStorageResponse
    {
        public int FileId { get; set; }

        public string? ObjectType { get; set; }

        public int? ObjectId { get; set; }

        public string? FileName { get; set; }

        public string? FilePath { get; set; }

        public string? FileType { get; set; }

        public long? FileSize { get; set; }

        public int? UploadedByUserId { get; set; }

        public DateTime? UploadedDate { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public string? StorageKey { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public string? TenantCode { get; set; }
    }
}
