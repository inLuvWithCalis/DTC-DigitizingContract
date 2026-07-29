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
        /// Lấy danh sách hợp đồng mà nhân viên đăng nhập đang phụ trách.
        /// Có tìm kiếm, lọc và phân trang.
        /// </summary>
        /// <remarks>
        /// Ví dụ:
        /// GET /api/contracts?page=1&amp;pageSize=20&amp;keyword=FPT&amp;status=0&amp;contractType=1
        /// </remarks>
        [HttpGet]
        [ProducesResponseType(
            typeof(ApiResponse<PagedResult<ContractListItemResponse>>),
            StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetList(
            [FromQuery] ContractFilterRequest filter)
        {
            var employeeId =
                HttpContext.Session.GetInt32("EmployeeId");

            if (employeeId is null)
            {
                throw new UnauthorizedAccessException(
                    "Bạn chưa đăng nhập hoặc session đã hết hạn.");
            }

            var result = await _contractService.GetListAsync(
                filter,
                employeeId.Value);

            return Ok(
                ApiResponse<PagedResult<ContractListItemResponse>>.Ok(
                    result,
                    "Lấy danh sách hợp đồng thành công."));
        }

        /// <summary>
        /// Lấy danh sách hợp đồng gốc đủ điều kiện
        /// để tạo hợp đồng bảo trì hoặc duy trì.
        /// </summary>
        /// <remarks>
        /// API này chỉ cung cấp dữ liệu cho dropdown/autocomplete.
        /// Backend vẫn kiểm tra lại hợp đồng nguồn khi tạo Contract Draft.
        ///
        /// Ví dụ:
        /// GET /api/contracts/eligible-parents?customerId=10&amp;targetContractType=2&amp;page=1&amp;pageSize=20
        /// </remarks>
        [HttpGet("eligible-parents")]
        [ProducesResponseType(
            typeof(ApiResponse<
                PagedResult<EligibleParentContractResponse>>),
            StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetEligibleParents(
            [FromQuery] EligibleParentContractFilterRequest filter)
        {
            var employeeId =
                HttpContext.Session.GetInt32("EmployeeId");

            if (employeeId is null)
            {
                throw new UnauthorizedAccessException(
                    "Bạn chưa đăng nhập hoặc session đã hết hạn.");
            }

            var result =
                await _contractService.GetEligibleParentsAsync(
                    filter,
                    employeeId.Value);

            return Ok(
                ApiResponse<
                    PagedResult<EligibleParentContractResponse>>.Ok(
                        result,
                        "Lấy danh sách hợp đồng nguồn thành công."));
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

        /// <summary>
        /// Cập nhật toàn bộ nội dung của hợp đồng Draft.
        /// </summary>
        [HttpPut("{contractId:int}/draft")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(
            typeof(ApiResponse<ContractDetailResponse>),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        [ProducesResponseType(
            StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateDraft(
            int contractId,
            [FromBody] UpdateContractDraftRequest request)
        {
            var employeeId =
                HttpContext.Session.GetInt32("EmployeeId");

            if (employeeId is null)
            {
                throw new UnauthorizedAccessException(
                    "Bạn chưa đăng nhập hoặc session đã hết hạn.");
            }

            var result = await _contractService.UpdateDraftAsync(
                contractId,
                request,
                employeeId.Value);

            return Ok(
                ApiResponse<ContractDetailResponse>.Ok(
                    result,
                    "Cập nhật hợp đồng nháp thành công."));
        }

        /// <summary>
        /// Chuyển hợp đồng từ Draft sang Negotiating.
        /// Version vẫn được phép chỉnh sửa.
        /// </summary>
        [HttpPost("{contractId:int}/start-negotiation")]
        [ProducesResponseType(
            typeof(ApiResponse<ContractDetailResponse>),
            StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> StartNegotiation(
            int contractId,
            [FromBody] StartContractNegotiationRequest request)
        {
            var employeeId =
                HttpContext.Session.GetInt32("EmployeeId");

            if (employeeId is null)
            {
                throw new UnauthorizedAccessException(
                    "Bạn chưa đăng nhập hoặc session đã hết hạn.");
            }

            var result =
                await _contractService.StartNegotiationAsync(
                    contractId,
                    request,
                    employeeId.Value);

            return Ok(
                ApiResponse<ContractDetailResponse>.Ok(
                    result,
                    "Hợp đồng đã chuyển sang giai đoạn đàm phán."));
        }

        /// <summary>
        /// Gửi version hiện hành đi duyệt và khóa snapshot.
        /// </summary>
        [HttpPost("{contractId:int}/submit-approval")]
        [ProducesResponseType(
            typeof(ApiResponse<SubmitContractForApprovalResponse>),
            StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> SubmitApproval(
            int contractId,
            [FromBody] SubmitContractForApprovalRequest request)
        {
            var employeeId =
                HttpContext.Session.GetInt32("EmployeeId");

            if (employeeId is null)
            {
                throw new UnauthorizedAccessException(
                    "Bạn chưa đăng nhập hoặc session đã hết hạn.");
            }

            var result =
                await _contractService.SubmitForApprovalAsync(
                    contractId,
                    request,
                    employeeId.Value);

            return Ok(
                ApiResponse<SubmitContractForApprovalResponse>.Ok(
                    result,
                    "Gửi hợp đồng duyệt thành công."));
        }
    }
}