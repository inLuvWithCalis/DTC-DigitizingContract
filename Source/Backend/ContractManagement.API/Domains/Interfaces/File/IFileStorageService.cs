using ContractManagement.Domains.DTOs.Responses.File;

namespace ContractManagement.Domains.Interfaces.File
{
    /// <summary>
    /// Service quản lý file dùng chung toàn hệ thống.
    /// Ví dụ: file hợp đồng, file hóa đơn, file khách hàng, file chứng từ.
    /// </summary>
    public interface IFileStorageService
    {
        Task<FileStorageResponse> UploadAsync(
            IFormFile file,
            string objectType,
            int objectId,
            int uploadedBy);

        Task<(Stream Stream, string FileName)?> DownloadAsync(int fileId);

        Task<List<FileStorageResponse>> GetByObjectAsync(
            string objectType,
            int objectId);

        Task DeleteAsync(int fileId);

        /// <summary>
        /// Bù trừ artifact vừa upload khi transaction nghiệp vụ không thể commit.
        /// Dùng FilePath đã được storage sinh ra, nên vẫn dọn physical file khi
        /// metadata đã bị database rollback.
        /// </summary>
        Task DeleteUploadedArtifactAsync(FileStorageResponse file);
    }
}
