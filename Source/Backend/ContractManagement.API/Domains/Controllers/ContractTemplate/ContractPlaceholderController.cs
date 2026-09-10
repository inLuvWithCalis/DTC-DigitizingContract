using ContractManagement.API.Common.Responses;
using ContractManagement.API.Common.Security;
using ContractManagement.API.Domains.DTOs.Requests.ContractTemplate;
using ContractManagement.Domains.Interfaces.ContractTemplate;
using ContractManagement.Domains.Policies.ContractTemplate;
using ContractManagement.Domains.Services.ContractTemplate;
using ContractManagement.Filter;
using Microsoft.AspNetCore.Mvc;

namespace ContractManagement.Domains.Controllers.ContractTemplate;

[ApiController]
[Route("api/contract-templates")]
[SessionAuthorize(RbacPermissions.TemplateManage)]
public sealed class ContractPlaceholderController(ContractPlaceholderDefinitionService service,
    IContractPlaceholderSourceRegistry registry) : ControllerBase
{
    [HttpGet("placeholder-source-fields")]
    public Task<IActionResult> Sources(CancellationToken ct) => Execute(async actor =>
    {
        await service.AuthorizeAsync(actor, ct);
        return registry.GetAllFields();
    });

    [HttpPost("placeholders")]
    public Task<IActionResult> Create(SaveContractPlaceholderRequest request, CancellationToken ct) =>
        Execute(actor => service.SaveAsync(null, request, actor, ct));

    [HttpPut("placeholders/{id:int}")]
    public Task<IActionResult> Update(int id, SaveContractPlaceholderRequest request, CancellationToken ct) =>
        Execute(actor => service.SaveAsync(id, request, actor, ct));

    [HttpPost("placeholders/{id:int}/activate")]
    public Task<IActionResult> Activate(int id, PlaceholderRowVersionRequest request, CancellationToken ct) =>
        Execute(actor => service.SetActiveAsync(id, true, request.RowVersion, actor, ct));

    [HttpPost("placeholders/{id:int}/deactivate")]
    public Task<IActionResult> Deactivate(int id, PlaceholderRowVersionRequest request, CancellationToken ct) =>
        Execute(actor => service.SetActiveAsync(id, false, request.RowVersion, actor, ct));

    [HttpGet("placeholders/{id:int}/usage")]
    public Task<IActionResult> Usage(int id, CancellationToken ct) => Execute(actor => service.UsageAsync(id, actor, ct));

    [HttpDelete("placeholders/{id:int}")]
    public Task<IActionResult> Delete(int id, PlaceholderRowVersionRequest request, CancellationToken ct) =>
        Execute(actor => service.DeleteAsync(id, request.RowVersion, actor, ct));

    private async Task<IActionResult> Execute<T>(Func<int, Task<T>> operation)
    {
        var actor = HttpContext.Session.GetInt32("EmployeeId");
        if (actor is null) return Unauthorized();
        try { return Ok(ApiResponse<T>.Ok(await operation(actor.Value))); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, ApiResponse<object>.Fail(ex.Message)); }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse<object>.Fail(ex.Message)); }
        catch (PlaceholderOperationException ex)
        {
            return StatusCode(ex.Code is "PlaceholderConcurrencyConflict" or "PlaceholderKeyAlreadyExists" or "PlaceholderInUse" ? 409 : 400,
                ApiResponse<object>.Fail(ex.Message, [ex.Code]));
        }
    }
}
