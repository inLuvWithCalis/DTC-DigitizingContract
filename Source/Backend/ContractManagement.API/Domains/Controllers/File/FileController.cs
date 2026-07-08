using ContractManagement.Common.Responses;
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
    [SessionAuthorize]
    public class FileController : ControllerBase
    {
        private readonly IFileStorageService _fileStorageService;

        public FileController(IFileStorageService fileStorageService)
        {
            _fileStorageService = fileStorageService;
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
            Console.WriteLine("API login was called");
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
            var employeeId = HttpContext.Session.GetInt32("EmployeeId");

            if (employeeId is null)
            {
                throw new UnauthorizedAccessException(
                    "Bạn chưa đăng nhập hoặc session đã hết hạn.");
            }

            var result = await _fileStorageService.UploadAsync(
                request.File,
                request.ObjectType,
                request.ObjectId,
                employeeId.Value);

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
            await _fileStorageService.DeleteAsync(fileId);

            return Ok(
                ApiResponse<object>.Ok(
                    new { fileId },
                    "Xóa file thành công."));
        }
    }
}