using ContractManagement.API.Common.Responses;
using ContractManagement.API.Common.Security;
using ContractManagement.Domains.DTOs.Requests.File;
using ContractManagement.Domains.DTOs.Responses.File;
using ContractManagement.Domains.Interfaces.Contract;
using ContractManagement.Domains.Interfaces.File;
using ContractManagement.Filter;
using ContractManagement.Infrastructure.Persistence.Application;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Domains.Controllers.File
{
    /// <summary>
    /// Controller quản lý file dùng chung toàn hệ thống.
    /// Ví dụ: file hợp đồng, hóa đơn, chứng từ, báo giá.
    /// </summary>
    [Route("api/files")]
    [ApiController]
    [SessionAuthorize(RbacPermissions.FileAccessByResource)]
    public class FileController : ControllerBase
    {
        private readonly IFileStorageService _fileStorageService;
        private readonly IFileResourceAuthorizationService _fileAuthorizationService;
        private readonly DbDtctechContext _dbContext;
        private readonly IContractAuditWriter _contractAuditWriter;

        public FileController(
            IFileStorageService fileStorageService,
            IFileResourceAuthorizationService fileAuthorizationService,
            DbDtctechContext dbContext,
            IContractAuditWriter contractAuditWriter)
        {
            _fileStorageService = fileStorageService;
            _fileAuthorizationService = fileAuthorizationService;
            _dbContext = dbContext;
            _contractAuditWriter = contractAuditWriter;
        }

        /// <summary>
        /// Upload file generic.
        /// Dùng form-data
        /// - file
        /// - objectType
        /// - objectId
        /// </summary>
        [HttpPost("upload")]
        public async Task<IActionResult> Upload([FromForm] UploadFileRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(x => x.Errors)
                    .Select(x => x.ErrorMessage)
                    .ToList();

                return BadRequest(
                    ApiResponse<object>.Fail(
                        "Dữ liệu upload không hợp lệ.",
                        errors));
            }

            // Lấy nhân viên đang login từ Session
            var employeeId = GetEmployeeId();

            await _fileAuthorizationService.EnsureCanWriteByObjectAsync(
                request.ObjectType,
                request.ObjectId,
                employeeId,
                HttpContext.RequestAborted);

            FileStorageResponse result;
            if (!string.Equals(
                    request.ObjectType,
                    ContractAuditSubjectTypes.Contract,
                    StringComparison.Ordinal))
            {
                result = await _fileStorageService.UploadAsync(
                    request.File,
                    request.ObjectType,
                    request.ObjectId,
                    employeeId);
            }
            else
            {
                result = await UploadContractFileWithAuditAsync(
                    request,
                    employeeId);
            }

            return Ok(
                ApiResponse<FileStorageResponse>.Ok(
                    result,
                    "Upload file thành công."));
        }

        /// <summary>
        /// Download file theo FileId.
        /// </summary>
        [HttpGet("{fileId:int}/download")]
        public async Task<IActionResult> Download(int fileId)
        {
            await _fileAuthorizationService.EnsureCanReadFileAsync(
                fileId,
                GetEmployeeId(),
                HttpContext.RequestAborted);

            var result = await _fileStorageService.DownloadAsync(fileId);

            if (result is null)
            {
                throw new KeyNotFoundException("Không tìm thấy file.");
            }

            return File(
                result.Value.Stream,
                "application/octet-stream",
                result.Value.FileName);
        }

        /// <summary>
        /// Lấy danh sách file theo objectType + objectId.
        /// Ví dụ: /api/files?objectType=Contract&amp;objectId=12
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetByObject(
            [FromQuery] string objectType,
            [FromQuery] int objectId)
        {
            if (string.IsNullOrWhiteSpace(objectType))
            {
                throw new ArgumentException("ObjectType không được để trống.");
            }

            if (objectId <= 0)
            {
                throw new ArgumentException("ObjectId không hợp lệ.");
            }

            await _fileAuthorizationService.EnsureCanReadByObjectAsync(
                objectType,
                objectId,
                GetEmployeeId(),
                HttpContext.RequestAborted);

            var result = await _fileStorageService.GetByObjectAsync(
                objectType,
                objectId);

            return Ok(
                ApiResponse<List<FileStorageResponse>>.Ok(
                    result,
                    "Lấy danh sách file thành công."));
        }

        /// <summary>
        /// Xóa file theo FileId.
        /// Xóa cả file vật lý và metadata trong database.
        /// </summary>
        [HttpDelete("{fileId:int}")]
        public async Task<IActionResult> Delete(int fileId)
        {
            var employeeId = GetEmployeeId();
            await _fileAuthorizationService.EnsureCanDeleteFileAsync(
                fileId,
                employeeId,
                HttpContext.RequestAborted);

            var file = await _dbContext.TblFileStorages
                .AsNoTracking()
                .Where(candidate => candidate.FileId == fileId)
                .Select(candidate => new
                {
                    candidate.FileId,
                    candidate.ObjectType,
                    candidate.ObjectId,
                    candidate.FileName,
                    candidate.UploadedDate
                })
                .SingleOrDefaultAsync(HttpContext.RequestAborted)
                ?? throw new KeyNotFoundException("Không tìm thấy file.");

            if (!string.Equals(
                    file.ObjectType,
                    ContractAuditSubjectTypes.Contract,
                    StringComparison.Ordinal))
            {
                await _fileStorageService.DeleteAsync(fileId);
            }
            else
            {
                var strategy = _dbContext.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    await using var transaction = await _dbContext.Database
                        .BeginTransactionAsync(HttpContext.RequestAborted);
                    try
                    {
                        _contractAuditWriter.StageEmployeeAudits(
                        [
                            new EmployeeContractAuditWriteRequest(
                                file.ObjectId,
                                null,
                                employeeId,
                                ContractAuditActionTypes.ContractAttachmentDeleted,
                                ContractAuditResults.Succeeded,
                                DateTime.UtcNow,
                                SubjectType: ContractAuditSubjectTypes.Contract,
                                SubjectId: file.ObjectId,
                                PreviousValues: ContractAuditValues.Create(
                                    ("FileId", file.FileId),
                                    ("FileName", file.FileName),
                                    ("UploadDate", file.UploadedDate)))
                        ]);
                        await _fileStorageService.DeleteAsync(fileId);
                        await _dbContext.SaveChangesAsync(
                            HttpContext.RequestAborted);
                        await transaction.CommitAsync(
                            HttpContext.RequestAborted);
                    }
                    catch
                    {
                        await RollbackAndClearAsync(transaction);
                        throw;
                    }
                });
            }

            return Ok(
                ApiResponse<object>.Ok(
                    new { fileId },
                    "Xóa file thành công."));
        }

        private int GetEmployeeId()
        {
            var employeeId = HttpContext.Session.GetInt32("EmployeeId");
            if (employeeId is null)
            {
                throw new UnauthorizedAccessException(
                    "Bạn chưa đăng nhập hoặc session đã hết hạn.");
            }

            return employeeId.Value;
        }

        private async Task<FileStorageResponse> UploadContractFileWithAuditAsync(
            UploadFileRequest request,
            int employeeId)
        {
            var strategy = _dbContext.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                FileStorageResponse? uploadedFile = null;
                await using var transaction = await _dbContext.Database
                    .BeginTransactionAsync(HttpContext.RequestAborted);
                try
                {
                    uploadedFile = await _fileStorageService.UploadAsync(
                        request.File,
                        request.ObjectType,
                        request.ObjectId,
                        employeeId);

                    _contractAuditWriter.StageEmployeeAudits(
                    [
                        new EmployeeContractAuditWriteRequest(
                            request.ObjectId,
                            null,
                            employeeId,
                            ContractAuditActionTypes.ContractAttachmentUploaded,
                            ContractAuditResults.Succeeded,
                            DateTime.UtcNow,
                            SubjectType: ContractAuditSubjectTypes.Contract,
                            SubjectId: request.ObjectId,
                            NewValues: ContractAuditValues.Create(
                                ("FileId", uploadedFile.FileId),
                                ("FileName", uploadedFile.FileName),
                                ("UploadDate", uploadedFile.UploadedDate)))
                    ]);
                    await _dbContext.SaveChangesAsync(
                        HttpContext.RequestAborted);
                    await transaction.CommitAsync(HttpContext.RequestAborted);
                    return uploadedFile;
                }
                catch
                {
                    await RollbackAndClearAsync(transaction);
                    if (uploadedFile is not null)
                    {
                        await _fileStorageService.DeleteUploadedArtifactAsync(
                            uploadedFile);
                    }

                    throw;
                }
            });
        }

        private async Task RollbackAndClearAsync(
            Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction)
        {
            try
            {
                await transaction.RollbackAsync(HttpContext.RequestAborted);
            }
            catch
            {
                // Giữ lại exception gốc để execution strategy quyết định retry.
            }

            _dbContext.ChangeTracker.Clear();
        }
    }
}
