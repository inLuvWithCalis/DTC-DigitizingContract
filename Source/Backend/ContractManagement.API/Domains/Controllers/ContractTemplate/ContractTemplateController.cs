using ContractManagement.API.Common.Responses;
using ContractManagement.API.Common.Security;
using ContractManagement.API.Domains.DTOs.Requests.ContractTemplate;
using ContractManagement.API.Domains.DTOs.Responses.ContractTemplate;
using ContractManagement.Domains.Interfaces.ContractTemplate;
using ContractManagement.Filter;
using Microsoft.AspNetCore.Mvc;

namespace ContractManagement.Domains.Controllers.ContractTemplate;

/// <summary>
/// API quản trị catalog/template/version draft của SoftwareSupply.
/// </summary>
[ApiController]
[Route("api/contract-templates")]
[SessionAuthorize(RbacPermissions.TemplateManage)]
public sealed class ContractTemplateController : ControllerBase
{
    private readonly IContractTemplateService _service;

    public ContractTemplateController(IContractTemplateService service)
    {
        _service = service;
    }

    /// <summary>
    /// GET /api/contract-templates/placeholder-catalog - đọc Catalog V1 cố định của SoftwareSupply.
    /// </summary>
    /// <remarks>
    /// Luồng: xác thực session và AdminOfficer active, sau đó trả danh sách placeholder hệ thống.
    /// Catalog chỉ đọc; tenant không được tự thêm key, đổi DataSource, requiredness hoặc multiplicity.
    /// Mục đích: làm nguồn sự thật để Slice 09 nhận diện và validate placeholder trong DOCX.
    /// </remarks>
    [HttpGet("placeholder-catalog")]
    public async Task<IActionResult> GetPlaceholderCatalog()
    {
        if (!TryGetEmployeeId(out var employeeId, out var unauthorized))
        {
            return unauthorized!;
        }

        try
        {
            var result = await _service.GetPlaceholderCatalogAsync(
                employeeId,
                HttpContext.RequestAborted);
            return Ok(ApiResponse<SoftwareSupplyPlaceholderCatalogResponse>.Ok(
                result,
                "Lấy catalog placeholder thành công."));
        }
        catch (UnauthorizedAccessException exception)
        {
            return Forbidden(exception);
        }
    }

    /// <summary>
    /// GET /api/contract-templates - liệt kê Template SoftwareSupply trong tenant hiện tại.
    /// </summary>
    /// <remarks>
    /// Luồng: xác thực actor, chuẩn hóa page/pageSize/keyword, lọc theo tenant và DocumentType,
    /// rồi trả metadata theo dạng phân trang. Route này phục vụ màn hình danh sách quản trị.
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] ContractTemplateFilterRequest filter,
        CancellationToken cancellationToken)
    {
        if (!TryGetEmployeeId(out var employeeId, out var unauthorized))
        {
            return unauthorized!;
        }

        try
        {
            var result = await _service.ListAsync(
                filter,
                employeeId,
                cancellationToken);
            return Ok(ApiResponse<PagedResult<ContractTemplateResponse>>.Ok(
                result,
                "Lấy danh sách template thành công."));
        }
        catch (UnauthorizedAccessException exception)
        {
            return Forbidden(exception);
        }
    }

    /// <summary>
    /// POST /api/contract-templates - tạo Template mới cùng Version 1 Draft.
    /// </summary>
    /// <remarks>
    /// Luồng: validate input và mã không trùng, server tự gán SoftwareSupplyContract, Active,
    /// VersionNo = 1, Draft và NotValidated, sau đó ghi Template + Version trong một transaction.
    /// Mục đích: khởi tạo authoring aggregate hoàn chỉnh, không để Template hoặc Version mồ côi.
    /// </remarks>
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateContractTemplateRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetEmployeeId(out var employeeId, out var unauthorized))
        {
            return unauthorized!;
        }

        try
        {
            var result = await _service.CreateAsync(
                request,
                employeeId,
                cancellationToken);
            return CreatedAtAction(
                nameof(Get),
                new { templateId = result.TemplateId },
                ApiResponse<ContractTemplateDetailResponse>.Ok(
                    result,
                    "Tạo template thành công."));
        }
        catch (UnauthorizedAccessException exception)
        {
            return Forbidden(exception);
        }
    }

    /// <summary>
    /// GET /api/contract-templates/{templateId} - xem metadata và lịch sử version của Template.
    /// </summary>
    /// <remarks>
    /// Luồng: xác thực actor, kiểm tra Template thuộc tenant và đúng loại SoftwareSupply,
    /// rồi trả metadata kèm các version summary. Đây là màn hình tổng quan, không thay đổi dữ liệu.
    /// </remarks>
    [HttpGet("{templateId:int}")]
    public async Task<IActionResult> Get(
        int templateId,
        CancellationToken cancellationToken)
    {
        if (!TryGetEmployeeId(out var employeeId, out var unauthorized))
        {
            return unauthorized!;
        }

        try
        {
            var result = await _service.GetAsync(
                templateId,
                employeeId,
                cancellationToken);
            return Ok(ApiResponse<ContractTemplateDetailResponse>.Ok(
                result,
                "Lấy chi tiết template thành công."));
        }
        catch (UnauthorizedAccessException exception)
        {
            return Forbidden(exception);
        }
    }

    /// <summary>
    /// PUT /api/contract-templates/{templateId} - cập nhật metadata hiển thị của Template.
    /// </summary>
    /// <remarks>
    /// Luồng: kiểm tra Template RowVersion mới nhất rồi chỉ sửa tên và mô tả trong transaction.
    /// TemplateCode, DocumentType, LanguageMode và IsActive không nằm trong request nên không thể
    /// bị đổi qua route này; stale RowVersion trả concurrency conflict để tránh ghi đè dữ liệu mới.
    /// </remarks>
    [HttpPut("{templateId:int}")]
    public async Task<IActionResult> Update(
        int templateId,
        [FromBody] UpdateContractTemplateRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetEmployeeId(out var employeeId, out var unauthorized))
        {
            return unauthorized!;
        }

        try
        {
            var result = await _service.UpdateAsync(
                templateId,
                request,
                employeeId,
                cancellationToken);
            return Ok(ApiResponse<ContractTemplateDetailResponse>.Ok(
                result,
                "Cập nhật metadata template thành công."));
        }
        catch (UnauthorizedAccessException exception)
        {
            return Forbidden(exception);
        }
    }

    /// <summary>
    /// GET /api/contract-templates/versions/{versionId} - xem chi tiết một Template Version.
    /// </summary>
    /// <remarks>
    /// Luồng: kiểm tra version thuộc Template SoftwareSupply trong tenant, sau đó trả trạng thái,
    /// validation metadata và các soft term theo DisplayOrder. Published/Retired chỉ được đọc.
    /// </remarks>
    [HttpGet("versions/{versionId:int}")]
    public async Task<IActionResult> GetVersion(
        int versionId,
        CancellationToken cancellationToken)
    {
        if (!TryGetEmployeeId(out var employeeId, out var unauthorized))
        {
            return unauthorized!;
        }

        try
        {
            var result = await _service.GetVersionAsync(
                versionId,
                employeeId,
                cancellationToken);
            return Ok(ApiResponse<ContractTemplateVersionDetailResponse>.Ok(
                result,
                "Lấy chi tiết template version thành công."));
        }
        catch (UnauthorizedAccessException exception)
        {
            return Forbidden(exception);
        }
    }

    /// <summary>
    /// POST /api/contract-templates/versions/{sourceVersionId}/copy - tạo Draft mới từ Published hiện hành.
    /// </summary>
    /// <remarks>
    /// Luồng: chỉ chấp nhận source là CurrentPublishedVersion, kiểm tra source RowVersion, tạo
    /// VersionNo tiếp theo và copy soft terms trong một transaction. File, hash, validation và
    /// extraction cũ không được copy; source Published vẫn giữ nguyên.
    /// </remarks>
    [HttpPost("versions/{sourceVersionId:int}/copy")]
    public async Task<IActionResult> CopyVersion(
        int sourceVersionId,
        [FromBody] CopyContractTemplateVersionRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetEmployeeId(out var employeeId, out var unauthorized))
        {
            return unauthorized!;
        }

        try
        {
            var result = await _service.CopyVersionAsync(
                sourceVersionId,
                request,
                employeeId,
                cancellationToken);
            return Ok(ApiResponse<ContractTemplateVersionDetailResponse>.Ok(
                result,
                "Copy template version thành công."));
        }
        catch (UnauthorizedAccessException exception)
        {
            return Forbidden(exception);
        }
    }

    /// <summary>
    /// POST /api/contract-templates/versions/{versionId}/document - upload DOCX Draft.
    /// </summary>
    /// <remarks>
    /// Chỉ nhận multipart/form-data tối đa 10 MiB. DOCX không an toàn/kỹ thuật
    /// bị từ chối hoàn toàn; DOCX an toàn nhưng sai catalog được lưu Invalid để
    /// khóa publish cho đến khi Admin Officer upload lại bản hợp lệ.
    /// </remarks>
    [HttpPost("versions/{versionId:int}/document")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 10 * 1024 * 1024)]
    [ProducesResponseType(typeof(ApiResponse<ContractTemplateVersionDetailResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UploadDocument(
        int versionId,
        [FromForm] UploadContractTemplateDocumentRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetEmployeeId(out var employeeId, out var unauthorized))
        {
            return unauthorized!;
        }

        try
        {
            var result = await _service.UploadDocumentAsync(
                versionId,
                request,
                employeeId,
                cancellationToken);
            return Ok(ApiResponse<ContractTemplateVersionDetailResponse>.Ok(
                result,
                "Upload và kiểm tra DOCX template thành công."));
        }
        catch (UnauthorizedAccessException exception)
        {
            return Forbidden(exception);
        }
    }

    /// <summary>
    /// POST /api/contract-templates/versions/{versionId}/preview - tạo DOCX preview mẫu cho Draft đã valid.
    /// </summary>
    /// <remarks>
    /// Actor phải là Admin Officer active trong tenant. Route chỉ nhận VersionRowVersion;
    /// renderer dùng Dataset V1 cố định, copy bytes từ DOCX nguồn sang artifact riêng và không
    /// đọc Contract/Customer/Tenant/nhân sự thật. Nếu fingerprint DOCX/catalog/dataset/language
    /// không đổi thì trả preview hiện có; nếu render mới, metadata Version và audit được commit
    /// trước khi artifact preview cũ bị xóa. Dynamic block sai bố cục trả PreviewLayoutUnsupported.
    /// </remarks>
    [HttpPost("versions/{versionId:int}/preview")]
    [ProducesResponseType(typeof(ApiResponse<ContractTemplatePreviewResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GeneratePreview(
        int versionId,
        [FromBody] GenerateContractTemplatePreviewRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetEmployeeId(out var employeeId, out var unauthorized))
        {
            return unauthorized!;
        }

        try
        {
            var result = await _service.GeneratePreviewAsync(
                versionId,
                request,
                employeeId,
                cancellationToken);
            return Ok(ApiResponse<ContractTemplatePreviewResponse>.Ok(
                result,
                result.IsReused
                    ? "Preview hiện hành vẫn còn hiệu lực."
                    : "Tạo DOCX preview mẫu thành công."));
        }
        catch (ContractTemplatePreviewException exception)
        {
            return BadRequest(ApiResponse<object>.Fail(
                exception.Message,
                [exception.FailureCode]));
        }
        catch (UnauthorizedAccessException exception)
        {
            return Forbidden(exception);
        }
    }

    /// <summary>
    /// GET /api/contract-templates/versions/{versionId}/preview - tải DOCX preview hiện hành.
    /// </summary>
    /// <remarks>
    /// Route thực hiện object authorization qua TemplateVersion tenant-scoped trước khi mở file.
    /// Không dùng generic file-download: preview không có, stale hoặc artifact không thuộc đúng
    /// Version đều trả lỗi nghiệp vụ và tuyệt đối không trả DOCX preview cũ.
    /// </remarks>
    [HttpGet("versions/{versionId:int}/preview")]
    [Produces("application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    public async Task<IActionResult> DownloadPreview(
        int versionId,
        CancellationToken cancellationToken)
    {
        if (!TryGetEmployeeId(out var employeeId, out var unauthorized))
        {
            return unauthorized!;
        }

        try
        {
            var result = await _service.DownloadPreviewAsync(
                versionId,
                employeeId,
                cancellationToken);
            return File(
                result.Stream,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                result.FileName);
        }
        catch (ContractTemplatePreviewException exception)
        {
            return BadRequest(ApiResponse<object>.Fail(
                exception.Message,
                [exception.FailureCode]));
        }
        catch (UnauthorizedAccessException exception)
        {
            return Forbidden(exception);
        }
    }

    /// <summary>
    /// POST /api/contract-templates/versions/{versionId}/publish - khóa Draft
    /// và lưu PDF của DOCX preview Dataset V1 hiện hành.
    /// </summary>
    [HttpPost("versions/{versionId:int}/publish")]
    public async Task<IActionResult> Publish(
        int versionId,
        [FromBody] PublishContractTemplateVersionRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetEmployeeId(out var employeeId, out var unauthorized))
        {
            return unauthorized!;
        }

        try
        {
            var result = await _service.PublishAsync(versionId, request,
                employeeId, cancellationToken);
            return Ok(ApiResponse<ContractTemplateVersionDetailResponse>.Ok(result,
                "Publish template version thành công."));
        }
        catch (ContractTemplatePdfRenderingException exception)
        {
            return BadRequest(ApiResponse<object>.Fail(exception.Message,
                [exception.FailureCode]));
        }
        catch (ContractTemplatePreviewException exception)
        {
            return BadRequest(ApiResponse<object>.Fail(exception.Message,
                [exception.FailureCode]));
        }
        catch (UnauthorizedAccessException exception)
        {
            return Forbidden(exception);
        }
    }

    /// <summary>
    /// POST /api/contract-templates/versions/{versionId}/retire - dừng dùng
    /// version Published cho Contract mới nhưng giữ nguyên mọi artifact.
    /// </summary>
    [HttpPost("versions/{versionId:int}/retire")]
    public async Task<IActionResult> Retire(
        int versionId,
        [FromBody] RetireContractTemplateVersionRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetEmployeeId(out var employeeId, out var unauthorized))
        {
            return unauthorized!;
        }

        try
        {
            var result = await _service.RetireAsync(versionId, request,
                employeeId, cancellationToken);
            return Ok(ApiResponse<ContractTemplateVersionDetailResponse>.Ok(result,
                "Retire template version thành công."));
        }
        catch (UnauthorizedAccessException exception)
        {
            return Forbidden(exception);
        }
    }

    /// <summary>
    /// GET /api/contract-templates/versions/{versionId}/preview/pdf - tải PDF
    /// preview bất biến của version Published hoặc Retired.
    /// </summary>
    [HttpGet("versions/{versionId:int}/preview/pdf")]
    [Produces("application/pdf")]
    public async Task<IActionResult> DownloadPublishedPreviewPdf(
        int versionId,
        CancellationToken cancellationToken)
    {
        if (!TryGetEmployeeId(out var employeeId, out var unauthorized))
        {
            return unauthorized!;
        }

        try
        {
            var result = await _service.DownloadPublishedPreviewPdfAsync(
                versionId, employeeId, cancellationToken);
            return File(result.Stream, "application/pdf", result.FileName);
        }
        catch (ContractTemplatePreviewException exception)
        {
            return BadRequest(ApiResponse<object>.Fail(exception.Message,
                [exception.FailureCode]));
        }
        catch (UnauthorizedAccessException exception)
        {
            return Forbidden(exception);
        }
    }

    /// <summary>
    /// POST /api/contract-templates/versions/{versionId}/terms - thêm soft term vào Draft Version.
    /// </summary>
    /// <remarks>
    /// Luồng: kiểm tra version là Draft, validate TermCode/title/order, bảo đảm TermCode và
    /// DisplayOrder duy nhất, rồi thêm term và cập nhật Version RowVersion trong transaction.
    /// Mục đích: xây dựng nội dung điều khoản mềm trước khi version được xử lý ở các Slice sau.
    /// </remarks>
    [HttpPost("versions/{versionId:int}/terms")]
    public async Task<IActionResult> AddTerm(
        int versionId,
        [FromBody] CreateContractTemplateTermRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetEmployeeId(out var employeeId, out var unauthorized))
        {
            return unauthorized!;
        }

        try
        {
            var result = await _service.AddTermAsync(
                versionId,
                request,
                employeeId,
                cancellationToken);
            return Ok(ApiResponse<ContractTemplateTermResponse>.Ok(
                result,
                "Thêm template term thành công."));
        }
        catch (UnauthorizedAccessException exception)
        {
            return Forbidden(exception);
        }
    }

    /// <summary>
    /// PUT /api/contract-templates/versions/{versionId}/terms/order - sắp xếp lại toàn bộ soft term.
    /// </summary>
    /// <remarks>
    /// Luồng: nhận đầy đủ term hiện có cùng RowVersion, kiểm tra không thiếu/dư/trùng term hoặc
    /// DisplayOrder, sau đó cập nhật toàn bộ trong một transaction. Có lỗi hoặc stale RowVersion
    /// thì rollback toàn bộ; route không tự chèn, xóa hoặc dồn thứ tự term.
    /// </remarks>
    [HttpPut("versions/{versionId:int}/terms/order")]
    public async Task<IActionResult> ReorderTerms(
        int versionId,
        [FromBody] ReorderContractTemplateTermsRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetEmployeeId(out var employeeId, out var unauthorized))
        {
            return unauthorized!;
        }

        try
        {
            var result = await _service.ReorderTermsAsync(
                versionId,
                request,
                employeeId,
                cancellationToken);
            return Ok(ApiResponse<ContractTemplateVersionDetailResponse>.Ok(
                result,
                "Sắp xếp template terms thành công."));
        }
        catch (UnauthorizedAccessException exception)
        {
            return Forbidden(exception);
        }
    }

    /// <summary>
    /// PUT /api/contract-templates/versions/{versionId}/terms/{termId} - sửa một soft term Draft.
    /// </summary>
    /// <remarks>
    /// Luồng: kiểm tra version là Draft, term thuộc đúng version, kiểm tra cả Version RowVersion
    /// và Term RowVersion, sau đó cập nhật nội dung, negotiable và DisplayOrder. Mục đích là ngăn
    /// sửa chéo tenant/version và ngăn lost update khi nhiều AdminOfficer cùng biên soạn.
    /// </remarks>
    [HttpPut("versions/{versionId:int}/terms/{termId:int}")]
    public async Task<IActionResult> UpdateTerm(
        int versionId,
        int termId,
        [FromBody] UpdateContractTemplateTermRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetEmployeeId(out var employeeId, out var unauthorized))
        {
            return unauthorized!;
        }

        try
        {
            var result = await _service.UpdateTermAsync(
                versionId,
                termId,
                request,
                employeeId,
                cancellationToken);
            return Ok(ApiResponse<ContractTemplateTermResponse>.Ok(
                result,
                "Cập nhật template term thành công."));
        }
        catch (UnauthorizedAccessException exception)
        {
            return Forbidden(exception);
        }
    }

    /// <summary>
    /// DELETE /api/contract-templates/versions/{versionId}/terms/{termId} - xóa một soft term Draft.
    /// </summary>
    /// <remarks>
    /// Luồng: kiểm tra version là Draft, term thuộc đúng version và cả hai RowVersion còn mới,
    /// rồi xóa trong transaction. Các term còn lại không tự động renumber; caller phải reorder
    /// rõ ràng nếu muốn thay đổi lại DisplayOrder.
    /// </remarks>
    [HttpDelete("versions/{versionId:int}/terms/{termId:int}")]
    public async Task<IActionResult> DeleteTerm(
        int versionId,
        int termId,
        [FromBody] DeleteContractTemplateTermRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetEmployeeId(out var employeeId, out var unauthorized))
        {
            return unauthorized!;
        }

        try
        {
            await _service.DeleteTermAsync(
                versionId,
                termId,
                request,
                employeeId,
                cancellationToken);
            return Ok(ApiResponse<object>.Ok(
                new { versionId, termId },
                "Xóa template term thành công."));
        }
        catch (UnauthorizedAccessException exception)
        {
            return Forbidden(exception);
        }
    }

    private bool TryGetEmployeeId(
        out int employeeId,
        out IActionResult? unauthorized)
    {
        var value = HttpContext.Session.GetInt32("EmployeeId");
        if (value is null)
        {
            employeeId = 0;
            unauthorized = Unauthorized(new
            {
                message = "Bạn chưa đăng nhập hoặc session đã hết hạn."
            });
            return false;
        }

        employeeId = value.Value;
        unauthorized = null;
        return true;
    }

    private IActionResult Forbidden(UnauthorizedAccessException exception) =>
        StatusCode(
            StatusCodes.Status403Forbidden,
            ApiResponse<object>.Fail(exception.Message));
}
