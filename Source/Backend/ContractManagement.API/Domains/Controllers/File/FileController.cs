using ContractManagement.API.Common.Responses;
using ContractManagement.API.Common.Security;
using ContractManagement.Domains.DTOs.Requests.File;
using ContractManagement.Domains.DTOs.Responses.File;
using ContractManagement.Domains.Interfaces.File;
using ContractManagement.Filter;
using Microsoft.AspNetCore.Mvc;

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

        public FileController(
            IFileStorageService fileStorageService,
            IFileResourceAuthorizationService fileAuthorizationService)
        {
            _fileStorageService = fileStorageService;
            _fileAuthorizationService = fileAuthorizationService;
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

            var result = await _fileStorageService.UploadAsync(
                request.File,
                request.ObjectType,
                request.ObjectId,
                employeeId);

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
        /// Ví dụ: /api/files?objectType=Contract&objectId=12
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
            await _fileAuthorizationService.EnsureCanDeleteFileAsync(
                fileId,
                GetEmployeeId(),
                HttpContext.RequestAborted);
            await _fileStorageService.DeleteAsync(fileId);

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
    }
}
