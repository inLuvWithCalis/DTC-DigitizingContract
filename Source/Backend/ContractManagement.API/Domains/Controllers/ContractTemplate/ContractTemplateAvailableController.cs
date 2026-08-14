using ContractManagement.API.Common.Responses;
using ContractManagement.API.Common.Security;
using ContractManagement.API.Domains.DTOs.Responses.ContractTemplate;
using ContractManagement.Domains.Interfaces.ContractTemplate;
using ContractManagement.Filter;
using Microsoft.AspNetCore.Mvc;

namespace ContractManagement.Domains.Controllers.ContractTemplate;

/// <summary>
/// Lookup endpoint for current published template versions available to an
/// active employee creating a contract.
/// </summary>
[ApiController]
[Route("api/contract-templates/available")]
[SessionAuthorize(RbacPermissions.TemplateAvailableRead)]
public sealed class ContractTemplateAvailableController : ControllerBase
{
    private readonly IContractTemplateService _service;

    public ContractTemplateAvailableController(IContractTemplateService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await _service.ListAvailableAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<AvailableContractTemplateVersionResponse>>.Ok(
            result,
            "Lấy template version có thể chọn thành công."));
    }
}
