using ContractManagement.API.Common.Responses;
using ContractManagement.API.Common.Security;
using ContractManagement.API.Domains.DTOs.Requests.CustomerInteraction;
using ContractManagement.API.Domains.DTOs.Responses.CustomerInteraction;
using ContractManagement.API.Domains.Interfaces.CustomerInteraction;
using ContractManagement.Filter;
using Microsoft.AspNetCore.Mvc;

namespace ContractManagement.Domains.Controllers.CRM
{
    [Route("api/customers/{customerId:int}/interactions")]
    [ApiController]
    [SessionAuthorize(RbacPermissions.CustomerManage)]
    public class CustomerInteractionController : ControllerBase
    {
        private readonly ICustomerInteractionService _service;

        public CustomerInteractionController(ICustomerInteractionService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            int customerId,
            [FromBody] CreateCustomerInteractionRequest request)
        {
            var employeeId = HttpContext.Session.GetInt32("EmployeeId");

            if (employeeId is null)
            {
                throw new UnauthorizedAccessException(
                    "Bạn chưa đăng nhập hoặc session đã hết hạn.");
            }

            var result = await _service.CreateAsync(
                customerId,
                request,
                employeeId.Value);

            return Ok(
                ApiResponse<CustomerInteractionResponse>.Ok(
                    result,
                    "Tạo lịch sử tương tác thành công."));
        }

        [HttpGet]
        public async Task<IActionResult> GetByCustomer(int customerId)
        {
            var result = await _service.GetByCustomerAsync(customerId);

            return Ok(
                ApiResponse<List<CustomerInteractionResponse>>.Ok(
                    result,
                    "Lấy lịch sử tương tác thành công."));
        }

        [HttpPut("{interactionId:int}")]
        public async Task<IActionResult> Update(
            int customerId,
            int interactionId,
            [FromBody] UpdateCustomerInteractionRequest request)
        {
            await _service.UpdateAsync(
                customerId,
                interactionId,
                request);

            return Ok(
                ApiResponse<object>.Ok(
                    new { customerId, interactionId },
                    "Cập nhật lịch sử tương tác thành công."));
        }
    }
}
