using ContractManagement.API.Common.Responses;
using ContractManagement.API.Domains.DTOs.Requests.Contract;
using ContractManagement.API.Domains.DTOs.Responses.Contract;
using ContractManagement.Domains.Interfaces.Contract;
using ContractManagement.Filter;
using Microsoft.AspNetCore.Mvc;

namespace ContractManagement.Domains.Controllers.Contract
{
    /// <summary>
    /// API quản lý hợp đồng.
    /// </summary>
    [Route("api/contracts")]
    [ApiController]
    [SessionAuthorize]
    public class ContractController : ControllerBase
    {
        private readonly IContractService _contractService;

        public ContractController(
            IContractService contractService)
        {
            _contractService = contractService;
        }

        /// <summary>
        /// Tạo một hợp đồng nháp mới.
        /// </summary>
        /// <remarks>
        /// API tự động thực hiện:
        ///
        /// - Tạo Contract ở trạng thái Draft.
        /// - Sinh mã hợp đồng.
        /// - Tạo Version 1.
        /// - Snapshot Product/Service.
        /// - Snapshot điều khoản từ template.
        /// - Tính tổng tiền.
        /// - Gán người tạo làm người phụ trách ban đầu.
        /// </remarks>
        /// <response code="201">
        /// Tạo hợp đồng nháp thành công.
        /// </response>
        /// <response code="400">
        /// Dữ liệu đầu vào hoặc business rule không hợp lệ.
        /// </response>
        /// <response code="401">
        /// Chưa đăng nhập hoặc session đã hết hạn.
        /// </response>
        /// <response code="404">
        /// Không tìm thấy Customer, Employee, Template hoặc hợp đồng nguồn.
        /// </response>
        [HttpPost]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(
            typeof(ApiResponse<CreateContractResponse>),
            StatusCodes.Status201Created)]
        [ProducesResponseType(
            typeof(ValidationProblemDetails),
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Create(
            [FromBody] CreateContractRequest request)
        {
            var employeeId =
                HttpContext.Session.GetInt32("EmployeeId");

            if (employeeId is null)
            {
                throw new UnauthorizedAccessException(
                    "Bạn chưa đăng nhập hoặc session đã hết hạn.");
            }

            var result = await _contractService.CreateAsync(
                request,
                employeeId.Value);

            return Created(
                $"/api/contracts/{result.ContractId}",
                ApiResponse<CreateContractResponse>.Ok(
                    result,
                    "Tạo hợp đồng nháp thành công."));
        }


        /// <summary>
        /// Lấy chi tiết hợp đồng cùng version hiện hành,
        /// items và các điều khoản snapshot.
        /// </summary>
        /// <response code="200">
        /// Lấy chi tiết hợp đồng thành công.
        /// </response>
        /// <response code="401">
        /// Chưa đăng nhập hoặc session đã hết hạn.
        /// </response>
        /// <response code="404">
        /// Không tìm thấy hợp đồng hoặc người dùng không có quyền xem.
        /// </response>
        [HttpGet("{contractId:int}")]
        [ProducesResponseType(
            typeof(ApiResponse<ContractDetailResponse>),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDetail(int contractId)
        {
            var employeeId =
                HttpContext.Session.GetInt32("EmployeeId");

            if (employeeId is null)
            {
                throw new UnauthorizedAccessException(
                    "Bạn chưa đăng nhập hoặc session đã hết hạn.");
            }

            var result = await _contractService.GetDetailAsync(
                contractId,
                employeeId.Value);

            return Ok(
                ApiResponse<ContractDetailResponse>.Ok(
                    result,
                    "Lấy chi tiết hợp đồng thành công."));
        }
    }
}