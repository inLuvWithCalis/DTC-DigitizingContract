using ContractManagement.Domains.DTOs.Responses.File;
using ContractManagement.Domains.Interfaces.File;
using ContractManagement.Infrastructure.MultiTenancy.Interfaces;
using ContractManagement.Infrastructure.Persistence.Application;
using ContractManagement.Infrastructure.Persistence.Application.Models;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Domains.Services.File
{
    /// <summary>
    /// Service lưu file local vào wwwroot/uploads.
    /// Đồng thời lưu metadata vào bảng tbl_FileStorage.
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

            // 3. Xác định wwwroot
            // Nếu chưa có wwwroot thì tự tạo.
            var webRootPath = _environment.WebRootPath;

            if (string.IsNullOrWhiteSpace(webRootPath))
            {
                webRootPath = Path.Combine(_environment.ContentRootPath, "wwwroot");
            }

            Directory.CreateDirectory(webRootPath);

            // 4. Tạo folder lưu file theo tenant/objectType/objectId
            var uploadFolder = Path.Combine(
                webRootPath,
                "uploads",
                tenant.TenantCode,
                objectType,
                objectId.ToString());

            Directory.CreateDirectory(uploadFolder);

            // 5. Tạo tên file vật lý để tránh trùng tên
            var originalFileName = Path.GetFileName(file.FileName);
            var storedFileName = $"{Guid.NewGuid():N}_{originalFileName}";

            var physicalPath = Path.Combine(uploadFolder, storedFileName);

            // 6. Lưu file vào ổ cứng
            await using (var stream = new FileStream(physicalPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // 7. Lưu đường dẫn tương đối vào DB
            // Không lưu full path ổ đĩa E:/C:/ để sau này deploy dễ hơn.
            var relativePath =
                $"/uploads/{tenant.TenantCode}/{objectType}/{objectId}/{storedFileName}";

            var fileEntity = new TblFileStorage
            {
                ObjectType = objectType,
                ObjectId = objectId,
                FileName = originalFileName,
                FilePath = relativePath,
                FileType = Path.GetExtension(originalFileName)
                    .TrimStart('.')
                    .ToLowerInvariant(),
                FileSize = file.Length,
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
                    if (System.IO.File.Exists(physicalPath))
                    {
                        System.IO.File.Delete(physicalPath);
                    }
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

            DeletePhysicalFile(file.FilePath);

            // 2. Xóa metadata trong DB
            _dbContext.TblFileStorages.Remove(file);
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteUploadedArtifactAsync(FileStorageResponse file)
        {
            ArgumentNullException.ThrowIfNull(file);

            // Transaction SQL có thể đã rollback metadata, nhưng physical file
            // không transactional nên phải được dọn từ safe relative path này.
            DeletePhysicalFile(file.FilePath);

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

        private void DeletePhysicalFile(string? filePath)
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
                UploadedDate = file.UploadedDate
            };
        }
    }
}
