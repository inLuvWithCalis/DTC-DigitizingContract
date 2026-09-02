using ContractManagement.Domains.DTOs.Responses.File;
using ContractManagement.Domains.Interfaces.File;
using ContractManagement.Infrastructure.MultiTenancy.Interfaces;
using ContractManagement.Infrastructure.Persistence.Application;
using ContractManagement.Infrastructure.Persistence.Application.Models;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Domains.Services.File
{
    /// <summary>
    /// Service lưu file mới vào private storage và metadata vào tbl_FileStorage.
    /// 
    /// Lưu ý multi-tenant:
    /// File được tách theo tenantCode để tránh lẫn file giữa các tenant.
    /// </summary>
    public class FileStorageService : IFileStorageService
    {
        private readonly DbDtctechContext _dbContext;
        private readonly IWebHostEnvironment _environment;
        private readonly ICurrentTenant _currentTenant;
        private readonly IPrivateFileStorage _privateFileStorage;

        public FileStorageService(
            DbDtctechContext dbContext,
            IWebHostEnvironment environment,
            ICurrentTenant currentTenant,
            IPrivateFileStorage privateFileStorage)
        {
            _dbContext = dbContext;
            _environment = environment;
            _currentTenant = currentTenant;
            _privateFileStorage = privateFileStorage;
        }

        public async Task<FileStorageResponse> UploadAsync(
            IFormFile file,
            string objectType,
            int objectId,
            int uploadedBy)
        {
            // 1. Validate file đầu vào
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File không hợp lệ.");
            }

            if (string.IsNullOrWhiteSpace(objectType))
            {
                throw new ArgumentException("ObjectType không được để trống.");
            }

            if (objectId <= 0)
            {
                throw new ArgumentException("ObjectId không hợp lệ.");
            }

            // 2. Lấy tenant hiện tại
            // Vì project là database-per-tenant, request nào upload file cũng phải resolve tenant.
            var tenant = _currentTenant.GetRequiredTenant();

            var originalFileName = Path.GetFileName(file.FileName);
            await using var content = file.OpenReadStream();
            var stored = await _privateFileStorage.SaveAsync(
                new PrivateFileSaveRequest(
                    content,
                    originalFileName,
                    file.ContentType,
                    file.Length,
                    tenant.TenantCode,
                    objectType,
                    objectId,
                    PrivateFileUploadPolicies.ContractAttachment()));

            var fileEntity = new TblFileStorage
            {
                ObjectType = objectType,
                ObjectId = objectId,
                FileName = originalFileName,
                // FilePath không còn là URL. Giữ storage key để các bảng legacy
                // đang liên kết bằng FilePath vẫn tìm được metadata.
                FilePath = stored.StorageKey,
                StorageKey = stored.StorageKey,
                ContentType = stored.ContentType,
                Sha256 = stored.Sha256,
                TenantCode = stored.TenantCode,
                FileType = Path.GetExtension(originalFileName)
                    .TrimStart('.')
                    .ToLowerInvariant(),
                FileSize = stored.FileSize,
                UploadedByUserId = uploadedBy,
                UploadedDate = DateTime.Now
            };

            try
            {
                _dbContext.TblFileStorages.Add(fileEntity);
                await _dbContext.SaveChangesAsync();
                return MapToResponse(fileEntity);
            }
            catch
            {
                // Disk is not transactional. If metadata cannot be persisted,
                // remove the just-written artifact before preserving the error.
                _dbContext.Entry(fileEntity).State = EntityState.Detached;
                try
                {
                    await _privateFileStorage.DeleteAsync(
                        stored.TenantCode,
                        stored.StorageKey);
                }
                catch
                {
                    // The caller keeps the persistence failure; orphan cleanup
                    // is intentionally best-effort in this shared service.
                }

                throw;
            }
        }

        public async Task<(Stream Stream, string FileName)?> DownloadAsync(int fileId)
        {
            // 1. Lấy metadata file trong tenant database hiện tại
            var file = await _dbContext.TblFileStorages
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.FileId == fileId);

            if (file == null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(file.StorageKey)
                && !string.IsNullOrWhiteSpace(file.TenantCode))
            {
                var privateStream = await _privateFileStorage.OpenReadAsync(
                    file.TenantCode,
                    file.StorageKey);
                return (privateStream, file.FileName ?? "download-file");
            }

            if (string.IsNullOrWhiteSpace(file.FilePath))
            {
                return null;
            }

            // 2. Chuyển relative path trong DB thành physical path
            var webRootPath = _environment.WebRootPath;

            if (string.IsNullOrWhiteSpace(webRootPath))
            {
                webRootPath = Path.Combine(_environment.ContentRootPath, "wwwroot");
            }

            var relativePath = file.FilePath.TrimStart('/')
                .Replace('/', Path.DirectorySeparatorChar);

            var physicalPath = Path.Combine(webRootPath, relativePath);

            if (!System.IO.File.Exists(physicalPath))
            {
                return null;
            }

            // 3. Trả stream cho controller download
            var stream = new FileStream(
                physicalPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);

            return (stream, file.FileName ?? "download-file");
        }

        public async Task<List<FileStorageResponse>> GetByObjectAsync(
            string objectType,
            int objectId)
        {
            var files = await _dbContext.TblFileStorages
                .AsNoTracking()
                .Where(x => x.ObjectType == objectType && x.ObjectId == objectId)
                .OrderByDescending(x => x.UploadedDate)
                .ToListAsync();

            return files.Select(MapToResponse).ToList();
        }

        public async Task DeleteAsync(int fileId)
        {
            var file = await _dbContext.TblFileStorages
                .FirstOrDefaultAsync(x => x.FileId == fileId);

            if (file == null)
            {
                throw new KeyNotFoundException("Không tìm thấy file.");
            }

            await DeletePhysicalFileAsync(file);

            // 2. Xóa metadata trong DB
            _dbContext.TblFileStorages.Remove(file);
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteUploadedArtifactAsync(FileStorageResponse file)
        {
            ArgumentNullException.ThrowIfNull(file);

            // Transaction SQL có thể đã rollback metadata, nhưng physical file
            // không transactional nên phải được dọn từ safe relative path này.
            if (!string.IsNullOrWhiteSpace(file.StorageKey)
                && !string.IsNullOrWhiteSpace(file.TenantCode))
            {
                await _privateFileStorage.DeleteAsync(
                    file.TenantCode,
                    file.StorageKey);
            }
            else
            {
                DeleteLegacyPhysicalFile(file.FilePath);
            }

            if (file.FileId <= 0)
            {
                return;
            }

            var metadata = await _dbContext.TblFileStorages
                .FirstOrDefaultAsync(candidate => candidate.FileId == file.FileId);
            if (metadata is null)
            {
                return;
            }

            _dbContext.TblFileStorages.Remove(metadata);
            await _dbContext.SaveChangesAsync();
        }

        private async Task DeletePhysicalFileAsync(TblFileStorage file)
        {
            if (!string.IsNullOrWhiteSpace(file.StorageKey)
                && !string.IsNullOrWhiteSpace(file.TenantCode))
            {
                await _privateFileStorage.DeleteAsync(file.TenantCode, file.StorageKey);
                return;
            }

            DeleteLegacyPhysicalFile(file.FilePath);
        }

        private void DeleteLegacyPhysicalFile(string? filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return;
            }

            var webRootPath = _environment.WebRootPath;
            if (string.IsNullOrWhiteSpace(webRootPath))
            {
                webRootPath = Path.Combine(_environment.ContentRootPath, "wwwroot");
            }

            var relativePath = filePath.TrimStart('/')
                .Replace('/', Path.DirectorySeparatorChar);
            var physicalPath = Path.Combine(webRootPath, relativePath);
            if (System.IO.File.Exists(physicalPath))
            {
                System.IO.File.Delete(physicalPath);
            }
        }

        private static FileStorageResponse MapToResponse(TblFileStorage file)
        {
            return new FileStorageResponse
            {
                FileId = file.FileId,
                ObjectType = file.ObjectType,
                ObjectId = file.ObjectId,
                FileName = file.FileName,
                FilePath = file.FilePath,
                FileType = file.FileType,
                FileSize = file.FileSize,
                UploadedByUserId = file.UploadedByUserId,
                UploadedDate = file.UploadedDate,
                StorageKey = file.StorageKey,
                TenantCode = file.TenantCode
            };
        }
    }
}
