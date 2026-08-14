using ContractManagement.API.Common.Responses;
using ContractManagement.API.Common.Security;
using ContractManagement.API.Domains.DTOs.Responses.Employee;
using ContractManagement.Domains.Interfaces.Employee;
using ContractManagement.Filter;
using Microsoft.AspNetCore.Mvc;

namespace ContractManagement.API.Domains.Controllers.Employee;

[ApiController]
[Route("api/employees")]
[SessionAuthorize(RbacPermissions.EmployeeDirectoryRead)]
public sealed class EmployeeDirectoryController : ControllerBase
{
    private readonly IEmployeeService _employeeService;

    public EmployeeDirectoryController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    [HttpGet("directory")]
    public async Task<IActionResult> GetDirectory()
    {
        var result = await _employeeService.GetDirectoryAsync();
        return Ok(ApiResponse<List<EmployeeDirectoryResponse>>.Ok(
            result,
            "Lấy employee directory thành công."));
    }
}
