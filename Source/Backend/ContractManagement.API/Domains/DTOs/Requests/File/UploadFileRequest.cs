using System.ComponentModel.DataAnnotations;

namespace ContractManagement.Domains.DTOs.Requests.File
{
    /// <summary>
    /// Request upload file dạng multipart/form-data.
    /// Dùng cho upload file generic: Contract, Invoice, Customer...
    /// </summary>
    public class UploadFileRequest
    {
        /// <summary>
        /// File upload từ frontend/Postman.
        /// </summary>
        [Required]
        public IFormFile File { get; set; } = default!;

        /// <summary>
        /// Loại object mà file này thuộc về.
        /// Ví dụ: Contract, Invoice, Customer.
        /// </summary>
        [Required]
        public string ObjectType { get; set; } = string.Empty;

        /// <summary>
        /// Id của object.
        /// Ví dụ ContractId = 12.
        /// </summary>
        [Range(1, int.MaxValue)]
        public int ObjectId { get; set; }
    }
}