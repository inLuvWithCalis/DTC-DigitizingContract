using ContractManagement.API.Common.Enums;
using ContractManagement.API.Common.Exceptions;
using ContractManagement.API.Common.Responses;
using ContractManagement.API.Domains.DTOs.Requests.ContractTemplate;
using ContractManagement.API.Domains.DTOs.Responses.ContractTemplate;
using ContractManagement.Common.Enums;
using ContractManagement.Domains.DTOs.Responses.File;
using ContractManagement.Domains.Interfaces.ContractTemplate;
using ContractManagement.Domains.Interfaces.File;
using ContractManagement.Domains.Policies.ContractTemplate;
using ContractManagement.Infrastructure.Persistence.Application;
using ContractManagement.Infrastructure.Persistence.Application.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace ContractManagement.Domains.Services.ContractTemplate;

/// <summary>
/// Quản trị metadata, draft version và soft terms của SoftwareSupply template.
/// DOCX, validation, publish và retire thuộc các slice sau.
/// </summary>
public sealed class ContractTemplateService : IContractTemplateService
{
    private static readonly Regex TemplateCodePattern = new(
        "^[A-Z0-9]+(?:[-_][A-Z0-9]+)*$",
        RegexOptions.CultureInvariant);

    private const byte ActiveEmployeeStatus = 1;
    private const string ContractTemplateVersionObjectType =
        "ContractTemplateVersion";
    private const string ContractTemplatePreviewObjectType =
        "ContractTemplatePreview";
    private const string ContractTemplatePublishedPreviewPdfObjectType =
        "ContractTemplatePublishedPreviewPdf";
    private static long _syntheticRowVersionSeed = 10_000;

    private readonly DbDtctechContext _dbContext;
    private readonly IContractPlaceholderCatalog _placeholderCatalog;
    private readonly IContractPlaceholderSourceRegistry _sourceRegistry;
    private readonly IFileStorageService? _fileStorageService;
    private readonly IContractTemplateDocumentValidator _documentValidator;
    private readonly IContractTemplateAuditWriter? _templateAuditWriter;
    private readonly IContractTemplatePreviewRenderer _previewRenderer;
    private readonly IContractTemplatePdfRenderer? _pdfRenderer;
    private readonly ILogger<ContractTemplateService>? _logger;

    public ContractTemplateService(
        DbDtctechContext dbContext,
        IFileStorageService? fileStorageService = null,
        IContractTemplateDocumentValidator? documentValidator = null,
        IContractTemplateAuditWriter? templateAuditWriter = null,
        IContractTemplatePreviewRenderer? previewRenderer = null,
        IContractTemplatePdfRenderer? pdfRenderer = null,
        ILogger<ContractTemplateService>? logger = null,
        IContractPlaceholderCatalog? placeholderCatalog = null,
        IContractPlaceholderSourceRegistry? sourceRegistry = null)
    {
        _dbContext = dbContext;
        _sourceRegistry = sourceRegistry ?? new ContractPlaceholderSourceRegistry();
        _placeholderCatalog = placeholderCatalog ?? new ContractPlaceholderCatalog(dbContext, _sourceRegistry);
        _fileStorageService = fileStorageService;
        _documentValidator = documentValidator
            ?? new ContractTemplateDocumentValidator(_placeholderCatalog);
        _templateAuditWriter = templateAuditWriter;
        _previewRenderer = previewRenderer
            ?? new ContractTemplatePreviewRenderer();
        _pdfRenderer = pdfRenderer;
        _logger = logger;
    }

    public async Task<IReadOnlyList<AvailableContractTemplateVersionResponse>>
        ListAvailableAsync(CancellationToken cancellationToken = default)
    {
        return await (
            from version in _dbContext.TblContractTemplateVersions.AsNoTracking()
            join template in _dbContext.TblContractTemplates.AsNoTracking()
                on version.TemplateId equals template.TemplateId
            where template.IsActive
                  && template.CurrentPublishedVersionId == version.TemplateVersionId
                  && version.Status == (byte)TemplateVersionStatus.Published
            orderby template.TemplateCode, version.VersionNo
            select new AvailableContractTemplateVersionResponse
            {
                TemplateId = template.TemplateId,
                TemplateCode = template.TemplateCode,
                TemplateName = template.TemplateName,
                TemplateNameEn = template.TemplateNameEn,
                DocumentType = (TemplateDocumentType)template.DocumentType,
                LanguageMode = (ContractLanguageMode)template.LanguageMode,
                TemplateVersionId = version.TemplateVersionId,
                VersionNo = version.VersionNo
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<AvailableContractTemplateVersionResponse>>
        SearchAvailableAsync(
            AvailableContractTemplateFilterRequest filter,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        var page = filter.Page <= 0 ? 1 : filter.Page;
        var pageSize = filter.PageSize <= 0 ? 20 : filter.PageSize;
        if (pageSize > 100)
        {
            throw new ArgumentException("PageSize không được vượt quá 100.");
        }

        var query =
            from version in _dbContext.TblContractTemplateVersions.AsNoTracking()
            join template in _dbContext.TblContractTemplates.AsNoTracking()
                on version.TemplateId equals template.TemplateId
            where template.IsActive
                  && template.CurrentPublishedVersionId == version.TemplateVersionId
                  && version.Status == (byte)TemplateVersionStatus.Published
            select new { version, template };

        var keyword = NormalizeOptional(filter.Keyword);
        if (keyword is not null)
        {
            query = query.Where(item =>
                item.template.TemplateCode.Contains(keyword)
                || item.template.TemplateName.Contains(keyword)
                || (item.template.TemplateNameEn != null
                    && item.template.TemplateNameEn.Contains(keyword)));
        }

        if (filter.DocumentType.HasValue)
        {
            query = query.Where(item =>
                item.template.DocumentType == (byte)filter.DocumentType.Value);
        }

        if (filter.LanguageMode.HasValue)
        {
            query = query.Where(item =>
                item.template.LanguageMode == (byte)filter.LanguageMode.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var offset = ((long)page - 1) * pageSize;
        if (offset > int.MaxValue)
        {
            throw new ArgumentException(
                "Requested template page is outside the supported range.");
        }
        var items = await query
            .OrderBy(item => item.template.TemplateCode)
            .ThenBy(item => item.version.VersionNo)
            .ThenBy(item => item.version.TemplateVersionId)
            .Skip((int)offset)
            .Take(pageSize)
            .Select(item => new AvailableContractTemplateVersionResponse
            {
                TemplateId = item.template.TemplateId,
                TemplateCode = item.template.TemplateCode,
                TemplateName = item.template.TemplateName,
                TemplateNameEn = item.template.TemplateNameEn,
                DocumentType = (TemplateDocumentType)item.template.DocumentType,
                LanguageMode = (ContractLanguageMode)item.template.LanguageMode,
                TemplateVersionId = item.version.TemplateVersionId,
                VersionNo = item.version.VersionNo
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<AvailableContractTemplateVersionResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<AvailableContractTemplateVersionDetailResponse>
        GetAvailableAsync(
            int templateVersionId,
            CancellationToken cancellationToken = default)
    {
        if (templateVersionId <= 0)
        {
            throw new ArgumentException("TemplateVersionId phải lớn hơn 0.");
        }

        var detail = await (
            from version in _dbContext.TblContractTemplateVersions.AsNoTracking()
            join template in _dbContext.TblContractTemplates.AsNoTracking()
                on version.TemplateId equals template.TemplateId
            where version.TemplateVersionId == templateVersionId
                  && template.IsActive
                  && template.CurrentPublishedVersionId == version.TemplateVersionId
                  && version.Status == (byte)TemplateVersionStatus.Published
            select new AvailableContractTemplateVersionDetailResponse
            {
                TemplateId = template.TemplateId,
                TemplateCode = template.TemplateCode,
                TemplateName = template.TemplateName,
                TemplateNameEn = template.TemplateNameEn,
                DocumentType = (TemplateDocumentType)template.DocumentType,
                LanguageMode = (ContractLanguageMode)template.LanguageMode,
                TemplateVersionId = version.TemplateVersionId,
                VersionNo = version.VersionNo
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (detail is null)
        {
            throw new KeyNotFoundException(
                "Không tìm thấy template version đã phát hành hiện hành.");
        }

        detail.Terms = await _dbContext.TblContractTemplateTerms
            .AsNoTracking()
            .Where(term => term.TemplateVersionId == templateVersionId)
            .OrderBy(term => term.DisplayOrder)
            .ThenBy(term => term.TemplateTermId)
            .Select(term => new AvailableContractTemplateTermResponse
            {
                TemplateTermId = term.TemplateTermId,
                TermCode = term.TermCode,
                TermTitle = term.TermTitle,
                TermTitleEn = term.TermTitleEn,
                TermContent = term.TermContent,
                TermContentEn = term.TermContentEn,
                IsNegotiable = term.IsNegotiable,
                DisplayOrder = term.DisplayOrder,
                TermKind = (ContractTermKind)term.TermKind
            })
            .ToListAsync(cancellationToken);

        var availableMilestones = await _dbContext.TblContractTemplatePaymentMilestones
            .AsNoTracking()
            .Where(item => item.TemplateVersionId == templateVersionId)
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.TemplatePaymentMilestoneId)
            .ToListAsync(cancellationToken);
        foreach (var term in detail.Terms)
        {
            term.PaymentMilestones = availableMilestones
                .Where(item => item.TemplateTermId == term.TemplateTermId)
                .Select(MapPaymentMilestone)
                .ToList();
        }

        return detail;
    }

    public async Task<SoftwareSupplyPlaceholderCatalogResponse>
        GetPlaceholderCatalogAsync(
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        await EnsureAdminOfficerAsync(employeeId, cancellationToken);
        var items = await _placeholderCatalog.GetAsync(true, cancellationToken);
        return new SoftwareSupplyPlaceholderCatalogResponse
        {
            CatalogVersion = ContractPlaceholderCatalog.Fingerprint(items),
            CustomEnabled = _placeholderCatalog is ContractPlaceholderCatalog { CustomEnabled: true },
            Items = items
        };
    }

    public async Task<PagedResult<ContractTemplateResponse>> ListAsync(
        ContractTemplateFilterRequest filter,
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        await EnsureAdminOfficerAsync(employeeId, cancellationToken);

        var page = filter.Page <= 0 ? 1 : filter.Page;
        var pageSize = filter.PageSize <= 0 ? 20 : filter.PageSize;
        if (pageSize > 100)
        {
            throw new ArgumentException("PageSize không được vượt quá 100.");
        }

        var query = _dbContext.TblContractTemplates
            .AsNoTracking()
            .Where(template =>
                template.DocumentType
                    == (byte)TemplateDocumentType.SoftwareSupplyContract);

        var keyword = NormalizeOptional(filter.Keyword);
        if (keyword is not null)
        {
            query = query.Where(template =>
                template.TemplateCode.Contains(keyword)
                || template.TemplateName.Contains(keyword)
                || (template.TemplateNameEn != null
                    && template.TemplateNameEn.Contains(keyword)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var offset = ((long)page - 1) * pageSize;
        if (offset > int.MaxValue)
        {
            throw new ArgumentException(
                "Requested template page is outside the supported range.");
        }

        var templates = await query
            .OrderByDescending(template => template.TemplateId)
            .Skip((int)offset)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ContractTemplateResponse>
        {
            Items = templates.Select(MapTemplate).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ContractTemplateDetailResponse> GetAsync(
        int templateId,
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        await EnsureAdminOfficerAsync(employeeId, cancellationToken);
        var template = await GetTemplateAsync(templateId, cancellationToken);
        return await LoadTemplateDetailAsync(template, cancellationToken);
    }

    public async Task<ContractTemplateDetailResponse> CreateAsync(
        CreateContractTemplateRequest request,
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await EnsureAdminOfficerAsync(employeeId, cancellationToken);

        var templateCode = NormalizeTemplateCode(request.TemplateCode);
        var templateName = NormalizeRequired(
            request.TemplateName,
            500,
            nameof(request.TemplateName));
        var templateNameEn = NormalizeOptional(request.TemplateNameEn, 500);
        var description = NormalizeOptional(request.Description, 2000);
        var changeNote = NormalizeOptional(request.InitialChangeNote, 2000);
        ValidateLanguageMode(request.LanguageMode);

        try
        {
            return await ExecuteInTransactionAsync(async () =>
            {
                await EnsureAdminOfficerAsync(employeeId, cancellationToken);
                var duplicate = await _dbContext.TblContractTemplates
                    .AnyAsync(
                        template => template.TemplateCode == templateCode,
                        cancellationToken);
                if (duplicate)
                {
                    throw new ArgumentException(
                        "TemplateCode đã tồn tại trong tenant.");
                }

                var now = DateTime.UtcNow;
                var template = new TblContractTemplate
                {
                    TemplateCode = templateCode,
                    TemplateName = templateName,
                    TemplateNameEn = templateNameEn,
                    DocumentType = (byte)TemplateDocumentType.SoftwareSupplyContract,
                    LanguageMode = (byte)request.LanguageMode,
                    Description = description,
                    CurrentPublishedVersionId = null,
                    IsActive = true,
                    CreatedEmployeeId = employeeId,
                    CreatedDate = now
                };
                SetSyntheticRowVersionIfNeeded(template);
                _dbContext.TblContractTemplates.Add(template);
                await _dbContext.SaveChangesAsync(cancellationToken);

                var version = new TblContractTemplateVersion
                {
                    TemplateId = template.TemplateId,
                    VersionNo = 1,
                    ChangeNote = changeNote,
                    Status = (byte)TemplateVersionStatus.Draft,
                    ValidationStatus = (byte)TemplateValidationStatus.NotValidated,
                    CreatedEmployeeId = employeeId,
                    CreatedDate = now
                };
                SetSyntheticRowVersionIfNeeded(version);
                _dbContext.TblContractTemplateVersions.Add(version);
                await _dbContext.SaveChangesAsync(cancellationToken);

                return await LoadTemplateDetailAsync(template, cancellationToken);
            }, cancellationToken);
        }
        catch (DbUpdateException exception)
            when (IsTemplateCodeUniqueViolation(exception))
        {
            throw new ArgumentException(
                "TemplateCode đã tồn tại trong tenant.",
                nameof(request.TemplateCode),
                exception);
        }
    }

    public async Task<ContractTemplateDetailResponse> UpdateAsync(
        int templateId,
        UpdateContractTemplateRequest request,
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await EnsureAdminOfficerAsync(employeeId, cancellationToken);

        var templateName = NormalizeRequired(
            request.TemplateName,
            500,
            nameof(request.TemplateName));
        var templateNameEn = NormalizeOptional(request.TemplateNameEn, 500);
        var description = NormalizeOptional(request.Description, 2000);
        var expectedRowVersion = DecodeRowVersion(
            request.RowVersion,
            nameof(request.RowVersion));

        return await ExecuteInTransactionAsync(async () =>
        {
            await EnsureAdminOfficerAsync(employeeId, cancellationToken);
            var template = await GetTemplateAsync(templateId, cancellationToken);
            EnsureRowVersionMatches(
                template.RowVersion,
                expectedRowVersion,
                "Template");
            SetOriginalRowVersion(template, expectedRowVersion);

            template.TemplateName = templateName;
            template.TemplateNameEn = templateNameEn;
            template.Description = description;
            template.UpdatedEmployeeId = employeeId;
            template.UpdatedDate = DateTime.UtcNow;
            RotateTemplateRowVersionIfNeeded(template);

            await _dbContext.SaveChangesAsync(cancellationToken);
            return await LoadTemplateDetailAsync(template, cancellationToken);
        }, cancellationToken);
    }

    public async Task<ContractTemplateVersionDetailResponse> GetVersionAsync(
        int versionId,
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        await EnsureAdminOfficerAsync(employeeId, cancellationToken);
        return await LoadVersionDetailAsync(versionId, cancellationToken);
    }

    public async Task<ContractTemplateVersionDetailResponse> CopyVersionAsync(
        int sourceVersionId,
        CopyContractTemplateVersionRequest request,
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await EnsureAdminOfficerAsync(employeeId, cancellationToken);

        var expectedRowVersion = DecodeRowVersion(
            request.RowVersion,
            nameof(request.RowVersion));
        var changeNote = NormalizeOptional(request.ChangeNote, 2000);

        return await ExecuteInTransactionAsync(async () =>
        {
            await EnsureAdminOfficerAsync(employeeId, cancellationToken);

            var source = await _dbContext.TblContractTemplateVersions
                .SingleOrDefaultAsync(
                    version => version.TemplateVersionId == sourceVersionId,
                    cancellationToken);
            if (source is null)
            {
                throw new KeyNotFoundException(
                    "Không tìm thấy template version nguồn.");
            }

            var template = await GetTemplateAsync(
                source.TemplateId,
                cancellationToken);
            var latestRetiredVersionId = await _dbContext
                .TblContractTemplateVersions
                .AsNoTracking()
                .Where(version =>
                    version.TemplateId == template.TemplateId
                    && version.Status == (byte)TemplateVersionStatus.Retired)
                .OrderByDescending(version => version.VersionNo)
                .ThenByDescending(version => version.TemplateVersionId)
                .Select(version => (int?)version.TemplateVersionId)
                .FirstOrDefaultAsync(cancellationToken);
            var hasExistingDraft = await _dbContext
                .TblContractTemplateVersions
                .AsNoTracking()
                .AnyAsync(version =>
                    version.TemplateId == template.TemplateId
                    && version.Status == (byte)TemplateVersionStatus.Draft,
                    cancellationToken);
            if (hasExistingDraft)
            {
                throw CreateDraftVersionAlreadyExistsException();
            }

            var canCreateDraft = ContractTemplatePolicy.CanCreateDraftFromSource(
                (TemplateVersionStatus)source.Status,
                template.CurrentPublishedVersionId == source.TemplateVersionId,
                template.CurrentPublishedVersionId.HasValue,
                latestRetiredVersionId == source.TemplateVersionId,
                hasExistingDraft);
            if (!canCreateDraft)
            {
                throw new InvalidOperationException(
                    "Chỉ bản Published hiện hành hoặc bản Retired mới nhất của mẫu không còn bản phát hành mới được dùng để tạo Draft.");
            }

            EnsureRowVersionMatches(
                source.RowVersion,
                expectedRowVersion,
                "Template version");
            SetOriginalRowVersion(source, expectedRowVersion);

            var maxVersionNo = await _dbContext.TblContractTemplateVersions
                .Where(version => version.TemplateId == template.TemplateId)
                .Select(version => (int?)version.VersionNo)
                .MaxAsync(cancellationToken) ?? 0;
            var now = DateTime.UtcNow;
            var copy = new TblContractTemplateVersion
            {
                TemplateId = template.TemplateId,
                VersionNo = maxVersionNo + 1,
                ChangeNote = changeNote,
                Status = (byte)TemplateVersionStatus.Draft,
                ValidationStatus = (byte)TemplateValidationStatus.NotValidated,
                CreatedEmployeeId = employeeId,
                CreatedDate = now
            };
            SetSyntheticRowVersionIfNeeded(copy);
            _dbContext.TblContractTemplateVersions.Add(copy);

            template.UpdatedEmployeeId = employeeId;
            template.UpdatedDate = now;
            RotateTemplateRowVersionIfNeeded(template);
            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception)
                when (IsUniqueConstraintViolation(exception))
            {
                throw CreateDraftVersionAlreadyExistsException();
            }

            var sourceTerms = await _dbContext.TblContractTemplateTerms
                .AsNoTracking()
                .Where(term => term.TemplateVersionId == source.TemplateVersionId)
                .OrderBy(term => term.DisplayOrder)
                .ThenBy(term => term.TemplateTermId)
                .ToListAsync(cancellationToken);

            foreach (var sourceTerm in sourceTerms)
            {
                var copiedTerm = new TblContractTemplateTerm
                {
                    TemplateVersionId = copy.TemplateVersionId,
                    TermCode = sourceTerm.TermCode,
                    TermTitle = sourceTerm.TermTitle,
                    TermTitleEn = sourceTerm.TermTitleEn,
                    TermContent = sourceTerm.TermContent,
                    TermContentEn = sourceTerm.TermContentEn,
                    TermKind = sourceTerm.TermKind,
                    IsNegotiable = sourceTerm.IsNegotiable,
                    DisplayOrder = sourceTerm.DisplayOrder,
                    CreatedEmployeeId = employeeId,
                    CreatedDate = now
                };
                SetSyntheticRowVersionIfNeeded(copiedTerm);
                _dbContext.TblContractTemplateTerms.Add(copiedTerm);
            }


            await _dbContext.SaveChangesAsync(cancellationToken);
            var copiedTermsByCode = await _dbContext.TblContractTemplateTerms
                .Where(term => term.TemplateVersionId == copy.TemplateVersionId)
                .ToDictionaryAsync(term => term.TermCode, cancellationToken);
            var sourceMilestones = await _dbContext.TblContractTemplatePaymentMilestones
                .AsNoTracking()
                .Where(item => item.TemplateVersionId == source.TemplateVersionId)
                .OrderBy(item => item.DisplayOrder)
                .ToListAsync(cancellationToken);
            var sourceTermsById = sourceTerms.ToDictionary(item => item.TemplateTermId);
            foreach (var sourceMilestone in sourceMilestones)
            {
                var targetTerm = copiedTermsByCode[sourceTermsById[sourceMilestone.TemplateTermId].TermCode];
                var copiedMilestone = new TblContractTemplatePaymentMilestone
                {
                    TemplateVersionId = copy.TemplateVersionId,
                    TemplateTermId = targetTerm.TemplateTermId,
                    MilestoneCode = sourceMilestone.MilestoneCode,
                    TitleVi = sourceMilestone.TitleVi,
                    TitleEn = sourceMilestone.TitleEn,
                    PaymentPercent = sourceMilestone.PaymentPercent,
                    DueAnchor = sourceMilestone.DueAnchor,
                    DueOffsetDays = sourceMilestone.DueOffsetDays,
                    DayCountMode = sourceMilestone.DayCountMode,
                    ConditionVi = sourceMilestone.ConditionVi,
                    ConditionEn = sourceMilestone.ConditionEn,
                    DisplayOrder = sourceMilestone.DisplayOrder,
                    CreatedEmployeeId = employeeId,
                    CreatedDate = now
                };
                SetSyntheticRowVersionIfNeeded(copiedMilestone);
                _dbContext.TblContractTemplatePaymentMilestones.Add(copiedMilestone);
            }

            var sourceLegalBases = await _dbContext.TblContractTemplateLegalBases
                .AsNoTracking()
                .Where(item => item.TemplateVersionId == source.TemplateVersionId)
                .OrderBy(item => item.DisplayOrder)
                .ThenBy(item => item.TemplateLegalBasisId)
                .ToListAsync(cancellationToken);
            foreach (var sourceBasis in sourceLegalBases)
            {
                var copiedBasis = new TblContractTemplateLegalBasis
                {
                    TemplateVersionId = copy.TemplateVersionId,
                    BasisCode = sourceBasis.BasisCode,
                    ContentVi = sourceBasis.ContentVi,
                    ContentEn = sourceBasis.ContentEn,
                    DisplayOrder = sourceBasis.DisplayOrder,
                    CreatedEmployeeId = employeeId,
                    CreatedDate = now
                };
                SetSyntheticRowVersionIfNeeded(copiedBasis);
                _dbContext.TblContractTemplateLegalBases.Add(copiedBasis);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            return await LoadVersionDetailAsync(copy.TemplateVersionId, cancellationToken);
        }, cancellationToken);
    }

    public async Task<ContractTemplateVersionDetailResponse> UploadDocumentAsync(
        int versionId,
        UploadContractTemplateDocumentRequest request,
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await EnsureAdminOfficerAsync(employeeId, cancellationToken);
        EnsureDocumentStorageIsConfigured();

        var expectedRowVersion = DecodeRowVersion(
            request.VersionRowVersion,
            nameof(request.VersionRowVersion));
        var preflightVersion = await GetVersionForUploadPreflightAsync(
            versionId,
            cancellationToken);
        EnsureDraft(preflightVersion);

        try
        {
            EnsureRowVersionMatches(
                preflightVersion.RowVersion,
                expectedRowVersion,
                "Template version");
        }
        catch (DbUpdateConcurrencyException)
        {
            await RecordConcurrencyConflictAsync(
                preflightVersion,
                employeeId,
                request.File,
                cancellationToken);
            throw;
        }

        var validation = await _documentValidator.ValidateAsync(
            request.File,
            cancellationToken);
        if (!validation.IsTechnicallyAccepted)
        {
            _logger?.LogWarning(
                "Template DOCX upload rejected. VersionId={VersionId}, FailureCode={FailureCode}, FileSizeBytes={FileSizeBytes}",
                versionId,
                validation.FailureCode ?? "Unknown",
                validation.FileSizeBytes);
            await RecordTechnicalRejectionAsync(
                preflightVersion,
                employeeId,
                validation,
                cancellationToken);
            throw new ArgumentException(
                $"Tệp DOCX bị từ chối do không đạt yêu cầu kỹ thuật hoặc an toàn. Mã lỗi: {validation.FailureCode ?? "Unknown"}.",
                nameof(request.File));
        }

        var documentBytes = validation.DocumentBytes
            ?? throw new InvalidOperationException(
                "DOCX đã được chấp nhận kỹ thuật nhưng thiếu payload kiểm tra.");
        var documentHash = Convert.ToHexString(SHA256.HashData(documentBytes))
            .ToLowerInvariant();
        FileStorageResponse? uploadedArtifact = null;
        int? oldFileId = null;
        int? oldPreviewFileId = null;

        try
        {
            var result = await ExecuteInTransactionAsync(async () =>
            {
                // Re-check inside the transaction. Uploading a 10 MiB document
                // may take long enough for another Admin Officer to edit Draft.
                await EnsureAdminOfficerAsync(employeeId, cancellationToken);
                var version = await GetVersionForMutationAsync(
                    versionId,
                    cancellationToken);
                EnsureDraft(version);
                EnsureRowVersionMatches(
                    version.RowVersion,
                    expectedRowVersion,
                    "Template version");
                SetOriginalRowVersion(version, expectedRowVersion);

                oldFileId = version.DocumentFileId;
                oldPreviewFileId = version.PreviewFileId;
                var oldFile = oldFileId is > 0
                    ? await _dbContext.TblFileStorages
                        .AsNoTracking()
                        .SingleOrDefaultAsync(
                            file => file.FileId == oldFileId.Value,
                            cancellationToken)
                    : null;

                // Storage receives the exact in-memory bytes accepted by the
                // validator, never a second read of the untrusted HTTP stream.
                await using var source = new MemoryStream(documentBytes,
                    writable: false);
                var safeFile = new FormFile(
                    source,
                    baseStreamOffset: 0,
                    length: documentBytes.LongLength,
                    name: "File",
                    fileName: $"template-version-{versionId}.docx")
                {
                    Headers = new HeaderDictionary(),
                    ContentType =
                        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                };
                uploadedArtifact = await _fileStorageService!.UploadAsync(
                    safeFile,
                    ContractTemplateVersionObjectType,
                    versionId,
                    employeeId);

                await ReplaceFieldSnapshotAsync(
                    versionId,
                    validation,
                    employeeId,
                    cancellationToken);

                var now = DateTime.UtcNow;
                version.DocumentFileId = uploadedArtifact.FileId;
                version.DocumentHash = documentHash;
                version.PlaceholderBindingHash = ContractPlaceholderCatalog.Fingerprint(validation.Definitions ?? []);
                version.ValidationStatus = validation.IsCatalogValid
                    ? (byte)TemplateValidationStatus.Valid
                    : (byte)TemplateValidationStatus.Invalid;
                version.ValidationMessage = validation.IsCatalogValid
                    ? null
                    : validation.ValidationMessage;
                version.ValidatedByEmployeeId = employeeId;
                version.ValidatedDate = now;
                // DocumentHash is part of the preview fingerprint. The old
                // preview cannot be served after this Draft commit.
                version.PreviewFileId = null;
                TouchVersion(version, employeeId, now);
                RotateVersionRowVersionIfNeeded(version);

                StageDocumentAudits(
                    version,
                    employeeId,
                    oldFileId is > 0,
                    oldFile,
                    uploadedArtifact,
                    validation,
                    now);
                await _dbContext.SaveChangesAsync(cancellationToken);
                return await LoadVersionDetailAsync(versionId, cancellationToken);
            }, cancellationToken);

            // The old artifact is not touched until the new Version state and
            // audit have committed. A cleanup failure leaves a safe orphan only.
            if (oldFileId is > 0)
            {
                await DeleteOldArtifactAfterCommitAsync(oldFileId.Value);
            }
            if (oldPreviewFileId is > 0
                && oldPreviewFileId != oldFileId)
            {
                await DeleteOldArtifactAfterCommitAsync(oldPreviewFileId.Value);
            }

            return result;
        }
        catch (DbUpdateConcurrencyException)
        {
            await CompensateNewArtifactAsync(uploadedArtifact);
            await RecordConcurrencyConflictAsync(
                preflightVersion,
                employeeId,
                request.File,
                cancellationToken);
            throw;
        }
        catch
        {
            await CompensateNewArtifactAsync(uploadedArtifact);
            throw;
        }
    }

    public async Task<ContractTemplatePreviewResponse> GeneratePreviewAsync(
        int versionId,
        GenerateContractTemplatePreviewRequest request,
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await EnsureAdminOfficerAsync(employeeId, cancellationToken);
        EnsureDocumentStorageIsConfigured();

        var expectedRowVersion = DecodeRowVersion(
            request.VersionRowVersion,
            nameof(request.VersionRowVersion));
        var preflightVersion = await GetVersionForUploadPreflightAsync(
            versionId,
            cancellationToken);
        var preflightTemplate = await GetTemplateForPreviewAsync(
            preflightVersion.TemplateId,
            cancellationToken);
        var preflightInput = await LoadTemplatePreviewInputAsync(
            versionId, cancellationToken);

        string fingerprint;
        try
        {
            EnsureRowVersionMatches(
                preflightVersion.RowVersion,
                expectedRowVersion,
                "Template version");
            EnsurePreviewEligible(preflightVersion);
            fingerprint = CreatePreviewSourceHash(
                preflightVersion.DocumentHash!,
                (ContractLanguageMode)preflightTemplate.LanguageMode,
                preflightVersion.PlaceholderBindingHash,
                preflightInput.CanonicalHash);
        }
        catch (DbUpdateConcurrencyException)
        {
            await RecordPreviewConcurrencyConflictAsync(
                preflightVersion,
                employeeId,
                cancellationToken);
            throw;
        }
        catch (ContractTemplatePreviewException exception)
        {
            await RecordPreviewRejectionAsync(
                preflightVersion,
                employeeId,
                exception.FailureCode,
                cancellationToken);
            throw;
        }

        if (await HasCurrentPreviewAsync(
                preflightVersion,
                fingerprint,
                cancellationToken))
        {
            return MapPreviewResponse(preflightVersion, isReused: true);
        }

        byte[] sourceBytes;
        try
        {
            sourceBytes = await DownloadAndVerifySourceDocumentAsync(
                preflightVersion,
                cancellationToken);
        }
        catch (ContractTemplatePreviewException exception)
        {
            await RecordPreviewRejectionAsync(
                preflightVersion,
                employeeId,
                exception.FailureCode,
                cancellationToken);
            throw;
        }

        byte[] previewBytes;
        try
        {
            var fields = await _dbContext.TblContractTemplateFields.AsNoTracking()
                .Where(x => x.TemplateVersionId == versionId).ToListAsync(cancellationToken);
            var definitions = fields.Select(ContractPlaceholderCatalog.FromSnapshot).ToArray();
            var samples = definitions.Where(x => !x.IsSystem).ToDictionary(x => x.Key, x =>
            {
                var source = _sourceRegistry.GetRequired(x.SourceFieldKey!);
                return x.FormatString is null ? source.SampleValue : source.FormattedSamples[x.FormatString];
            }, StringComparer.Ordinal);
            previewBytes = _previewRenderer.RenderSample(
                sourceBytes,
                (ContractLanguageMode)preflightTemplate.LanguageMode,
                definitions.Length == 0 ? ContractPlaceholderCatalog.SystemDefinitions : definitions,
                samples,
                preflightInput.RenderData);
        }
        catch (ContractTemplatePreviewException exception)
        {
            await RecordPreviewRejectionAsync(
                preflightVersion,
                employeeId,
                exception.FailureCode,
                cancellationToken);
            throw;
        }

        FileStorageResponse? uploadedPreview = null;
        int? oldPreviewFileId = null;
        try
        {
            var result = await ExecuteInTransactionAsync(async () =>
            {
                await EnsureAdminOfficerAsync(employeeId, cancellationToken);
                var version = await GetVersionForMutationAsync(
                    versionId,
                    cancellationToken);
                var template = await GetTemplateAsync(
                    version.TemplateId,
                    cancellationToken);
                EnsureRowVersionMatches(
                    version.RowVersion,
                    expectedRowVersion,
                    "Template version");
                EnsurePreviewEligible(version);
                var currentInput = await LoadTemplatePreviewInputAsync(
                    versionId, cancellationToken);
                var currentFingerprint = CreatePreviewSourceHash(
                    version.DocumentHash!,
                    (ContractLanguageMode)template.LanguageMode,
                    version.PlaceholderBindingHash,
                    currentInput.CanonicalHash);
                if (!string.Equals(fingerprint, currentFingerprint,
                        StringComparison.Ordinal))
                {
                    throw new DbUpdateConcurrencyException(
                        "Template version đã thay đổi đầu vào preview.");
                }

                if (await HasCurrentPreviewAsync(
                        version,
                        currentFingerprint,
                        cancellationToken))
                {
                    return MapPreviewResponse(version, isReused: true);
                }

                SetOriginalRowVersion(version, expectedRowVersion);
                oldPreviewFileId = version.PreviewFileId;
                await using var previewStream = new MemoryStream(previewBytes,
                    writable: false);
                var previewFile = new FormFile(
                    previewStream,
                    baseStreamOffset: 0,
                    length: previewBytes.LongLength,
                    name: "Preview",
                    fileName: $"template-preview-{versionId}.docx")
                {
                    Headers = new HeaderDictionary(),
                    ContentType =
                        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                };
                uploadedPreview = await _fileStorageService!.UploadAsync(
                    previewFile,
                    ContractTemplatePreviewObjectType,
                    versionId,
                    employeeId);

                var now = DateTime.UtcNow;
                version.PreviewFileId = uploadedPreview.FileId;
                version.PreviewSourceHash = currentFingerprint;
                version.PreviewedAt = now;
                version.PreviewedByEmployeeId = employeeId;
                TouchVersion(version, employeeId, now);
                RotateVersionRowVersionIfNeeded(version);
                StagePreviewGeneratedAudit(
                    version,
                    employeeId,
                    oldPreviewFileId,
                    uploadedPreview,
                    now);
                await _dbContext.SaveChangesAsync(cancellationToken);
                return MapPreviewResponse(version, isReused: false);
            }, cancellationToken);

            // The prior preview is still addressable until Version metadata and
            // the audit have committed. A failed cleanup leaves only an orphan.
            if (uploadedPreview is not null && oldPreviewFileId is > 0
                && oldPreviewFileId != uploadedPreview.FileId)
            {
                await DeleteOldArtifactAfterCommitAsync(oldPreviewFileId.Value);
            }

            return result;
        }
        catch (DbUpdateConcurrencyException)
        {
            await CompensateNewArtifactAsync(uploadedPreview);
            await RecordPreviewConcurrencyConflictAsync(
                preflightVersion,
                employeeId,
                cancellationToken);
            throw;
        }
        catch
        {
            await CompensateNewArtifactAsync(uploadedPreview);
            throw;
        }
    }

    public async Task<(Stream Stream, string FileName)> DownloadPreviewAsync(
        int versionId,
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        await EnsureAdminOfficerAsync(employeeId, cancellationToken);
        EnsureDocumentStorageIsConfigured();

        var version = await GetVersionForUploadPreflightAsync(
            versionId,
            cancellationToken);
        var template = await GetTemplateForPreviewAsync(
            version.TemplateId,
            cancellationToken);
        EnsurePreviewDownloadEligible(version);
        var previewInput = await LoadTemplatePreviewInputAsync(
            versionId, cancellationToken);
        var fingerprint = CreatePreviewSourceHash(
            version.DocumentHash!,
            (ContractLanguageMode)template.LanguageMode,
            version.PlaceholderBindingHash,
            previewInput.CanonicalHash);

        if (version.PreviewFileId is not > 0)
        {
            throw new ContractTemplatePreviewException(
                version.PreviewSourceHash is null
                    ? "PreviewNotFound"
                    : "PreviewStale",
                version.PreviewSourceHash is null
                    ? "Template version chưa có preview hiện hành."
                    : "Preview hiện có đã stale và không thể tải.");
        }
        if (!string.Equals(version.PreviewSourceHash, fingerprint,
                StringComparison.Ordinal))
        {
            throw new ContractTemplatePreviewException(
                "PreviewStale",
                "Preview hiện có đã stale và không thể tải.");
        }

        var isOwnedPreview = await _dbContext.TblFileStorages
            .AsNoTracking()
            .AnyAsync(file => file.FileId == version.PreviewFileId.Value
                && file.ObjectType == ContractTemplatePreviewObjectType
                && file.ObjectId == versionId,
                cancellationToken);
        if (!isOwnedPreview)
        {
            throw new ContractTemplatePreviewException(
                "PreviewArtifactUnavailable",
                "Artifact preview hiện hành không còn khả dụng.");
        }

        var artifact = await _fileStorageService!.DownloadAsync(
            version.PreviewFileId.Value);
        if (artifact is null)
        {
            throw new ContractTemplatePreviewException(
                "PreviewArtifactUnavailable",
                "Artifact preview hiện hành không còn khả dụng.");
        }

        return (artifact.Value.Stream,
            $"template-preview-{versionId}.docx");
    }

    public async Task<ContractTemplateVersionDetailResponse> PublishAsync(
        int versionId,
        PublishContractTemplateVersionRequest request,
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await EnsureAdminOfficerAsync(employeeId, cancellationToken);
        EnsureDocumentStorageIsConfigured();
        var renderer = _pdfRenderer ?? throw new InvalidOperationException(
            "PDF renderer chưa được cấu hình cho Template publish.");
        var expectedRowVersion = DecodeRowVersion(request.VersionRowVersion,
            nameof(request.VersionRowVersion));
        var preflightVersion = await GetVersionForUploadPreflightAsync(versionId,
            cancellationToken);
        var preflightTemplate = await GetTemplateForPreviewAsync(
            preflightVersion.TemplateId, cancellationToken);
        var preflightInput = await LoadTemplatePreviewInputAsync(
            versionId, cancellationToken);
        string fingerprint;
        byte[] previewDocx;
        try
        {
            EnsureRowVersionMatches(preflightVersion.RowVersion, expectedRowVersion,
                "Template version");
            EnsurePublishEligible(preflightVersion);
            await ValidatePaymentTermsForPublishAsync(versionId, cancellationToken);
            fingerprint = CreatePreviewSourceHash(preflightVersion.DocumentHash!,
                (ContractLanguageMode)preflightTemplate.LanguageMode,
                preflightVersion.PlaceholderBindingHash,
                preflightInput.CanonicalHash);
            previewDocx = await DownloadCurrentPreviewBytesAsync(preflightVersion,
                fingerprint, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            await RecordPublishConcurrencyConflictAsync(preflightVersion, employeeId,
                cancellationToken);
            throw;
        }

        byte[] pdf;
        try
        {
            pdf = await renderer.ConvertPreviewToPdfAsync(previewDocx,
                cancellationToken);
        }
        catch (ContractTemplatePdfRenderingException exception)
        {
            await RecordPdfRenderFailureAsync(preflightVersion, employeeId,
                exception.FailureCode, cancellationToken);
            throw;
        }

        FileStorageResponse? uploadedPdf = null;
        try
        {
            await ExecuteInTransactionAsync(async () =>
            {
                await EnsureAdminOfficerAsync(employeeId, cancellationToken);
                var version = await GetVersionForMutationAsync(versionId,
                    cancellationToken);
                var template = await GetTemplateAsync(version.TemplateId,
                    cancellationToken);
                EnsureRowVersionMatches(version.RowVersion, expectedRowVersion,
                    "Template version");
                EnsurePublishEligible(version);
                await ValidatePaymentTermsForPublishAsync(versionId, cancellationToken);
                var currentInput = await LoadTemplatePreviewInputAsync(
                    versionId, cancellationToken);
                var currentFingerprint = CreatePreviewSourceHash(version.DocumentHash!,
                    (ContractLanguageMode)template.LanguageMode,
                    version.PlaceholderBindingHash,
                    currentInput.CanonicalHash);
                if (!string.Equals(fingerprint, currentFingerprint,
                        StringComparison.Ordinal))
                {
                    throw new DbUpdateConcurrencyException(
                        "Template version đã thay đổi đầu vào publish.");
                }
                await DownloadCurrentPreviewBytesAsync(version, currentFingerprint,
                    cancellationToken);

                SetOriginalRowVersion(version, expectedRowVersion);
                await using var pdfStream = new MemoryStream(pdf, writable: false);
                var pdfFile = new FormFile(pdfStream, 0, pdf.LongLength, "Pdf",
                    $"template-preview-{versionId}.pdf")
                {
                    Headers = new HeaderDictionary(),
                    ContentType = "application/pdf"
                };
                uploadedPdf = await _fileStorageService!.UploadAsync(pdfFile,
                    ContractTemplatePublishedPreviewPdfObjectType, versionId, employeeId);

                var now = DateTime.UtcNow;
                TblContractTemplateVersion? previous = null;
                if (template.CurrentPublishedVersionId is > 0
                    && template.CurrentPublishedVersionId != versionId)
                {
                    previous = await _dbContext.TblContractTemplateVersions
                        .SingleOrDefaultAsync(candidate =>
                            candidate.TemplateVersionId == template.CurrentPublishedVersionId.Value,
                            cancellationToken);
                    if (previous is null || previous.Status != (byte)TemplateVersionStatus.Published)
                    {
                        throw new DbUpdateConcurrencyException(
                            "Current Published Version không còn hợp lệ.");
                    }

                    previous.Status = (byte)TemplateVersionStatus.Retired;
                    TouchVersion(previous, employeeId, now);
                    RotateVersionRowVersionIfNeeded(previous);
                }

                version.PublishedPreviewPdfFileId = uploadedPdf.FileId;
                version.Status = (byte)TemplateVersionStatus.Published;
                version.PublishedByEmployeeId = employeeId;
                version.PublishedDate = now;
                TouchVersion(version, employeeId, now);
                RotateVersionRowVersionIfNeeded(version);
                template.CurrentPublishedVersionId = versionId;
                template.UpdatedEmployeeId = employeeId;
                template.UpdatedDate = now;
                RotateTemplateRowVersionIfNeeded(template);
                StagePublishAudits(version, previous, employeeId, uploadedPdf, now);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }, cancellationToken);

            return await LoadVersionDetailAsync(versionId, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            await CompensateNewArtifactAsync(uploadedPdf);
            await RecordPublishConcurrencyConflictAsync(preflightVersion, employeeId,
                cancellationToken);
            throw;
        }
        catch
        {
            await CompensateNewArtifactAsync(uploadedPdf);
            throw;
        }
    }

    public async Task<ContractTemplateVersionDetailResponse> RetireAsync(
        int versionId,
        RetireContractTemplateVersionRequest request,
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await EnsureAdminOfficerAsync(employeeId, cancellationToken);
        var expectedRowVersion = DecodeRowVersion(request.VersionRowVersion,
            nameof(request.VersionRowVersion));
        return await ExecuteInTransactionAsync(async () =>
        {
            await EnsureAdminOfficerAsync(employeeId, cancellationToken);
            var version = await GetVersionForMutationAsync(versionId,
                cancellationToken);
            var template = await GetTemplateAsync(version.TemplateId,
                cancellationToken);
            EnsureRowVersionMatches(version.RowVersion, expectedRowVersion,
                "Template version");
            if (version.Status != (byte)TemplateVersionStatus.Published)
            {
                throw new InvalidOperationException(
                    "Chỉ TemplateVersion Published mới có thể retire.");
            }

            SetOriginalRowVersion(version, expectedRowVersion);
            var now = DateTime.UtcNow;
            version.Status = (byte)TemplateVersionStatus.Retired;
            TouchVersion(version, employeeId, now);
            RotateVersionRowVersionIfNeeded(version);
            if (template.CurrentPublishedVersionId == versionId)
            {
                template.CurrentPublishedVersionId = null;
                template.UpdatedEmployeeId = employeeId;
                template.UpdatedDate = now;
                RotateTemplateRowVersionIfNeeded(template);
            }

            StageRetiredAudit(version, employeeId, now);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return await LoadVersionDetailAsync(versionId, cancellationToken);
        }, cancellationToken);
    }

    public async Task<(Stream Stream, string FileName)> DownloadPublishedPreviewPdfAsync(
        int versionId,
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        await EnsureAdminOfficerAsync(employeeId, cancellationToken);
        EnsureDocumentStorageIsConfigured();
        var version = await GetVersionForUploadPreflightAsync(versionId,
            cancellationToken);
        if (version.Status != (byte)TemplateVersionStatus.Published
            && version.Status != (byte)TemplateVersionStatus.Retired
            || version.PublishedPreviewPdfFileId is not > 0)
        {
            throw new ContractTemplatePreviewException("PublishedPdfNotFound",
                "Template version chưa có PDF preview đã publish.");
        }

        var owned = await _dbContext.TblFileStorages.AsNoTracking().AnyAsync(file =>
            file.FileId == version.PublishedPreviewPdfFileId.Value
            && file.ObjectType == ContractTemplatePublishedPreviewPdfObjectType
            && file.ObjectId == versionId, cancellationToken);
        if (!owned)
        {
            throw new ContractTemplatePreviewException("PublishedPdfUnavailable",
                "Artifact PDF preview đã publish không còn khả dụng.");
        }

        var artifact = await _fileStorageService!.DownloadAsync(
            version.PublishedPreviewPdfFileId.Value);
        if (artifact is null)
        {
            throw new ContractTemplatePreviewException("PublishedPdfUnavailable",
                "Artifact PDF preview đã publish không còn khả dụng.");
        }

        return (artifact.Value.Stream, $"template-preview-{versionId}.pdf");
    }

    public async Task<ContractTemplateTermResponse> AddTermAsync(
        int versionId,
        CreateContractTemplateTermRequest request,
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await EnsureAdminOfficerAsync(employeeId, cancellationToken);

        var values = NormalizeTerm(request.TermCode, request.TermTitle,
            request.TermTitleEn, request.TermContent, request.TermContentEn,
            request.DisplayOrder);
        var expectedVersionRowVersion = DecodeRowVersion(
            request.VersionRowVersion,
            nameof(request.VersionRowVersion));

        return await ExecuteInTransactionAsync(async () =>
        {
            await EnsureAdminOfficerAsync(employeeId, cancellationToken);
            var version = await GetVersionForMutationAsync(
                versionId,
                cancellationToken);
            EnsureDraft(version);
            EnsureRowVersionMatches(
                version.RowVersion,
                expectedVersionRowVersion,
                "Template version");
            SetOriginalRowVersion(version, expectedVersionRowVersion);
            await EnsureTermCodeAndDisplayOrderAreAvailableAsync(
                versionId,
                values.TermCode,
                values.DisplayOrder,
                null,
                cancellationToken);
            ValidateTermKind(request.TermKind);
            await EnsureSinglePaymentTermAsync(versionId, request.TermKind, null,
                cancellationToken);

            var now = DateTime.UtcNow;
            var term = new TblContractTemplateTerm
            {
                TemplateVersionId = versionId,
                TermCode = values.TermCode,
                TermTitle = values.TermTitle,
                TermTitleEn = values.TermTitleEn,
                TermContent = values.TermContent,
                TermContentEn = values.TermContentEn,
                TermKind = (byte)request.TermKind,
                IsNegotiable = request.IsNegotiable,
                DisplayOrder = values.DisplayOrder,
                CreatedEmployeeId = employeeId,
                CreatedDate = now
            };
            SetSyntheticRowVersionIfNeeded(term);
            _dbContext.TblContractTemplateTerms.Add(term);
            TouchVersion(version, employeeId, now);
            RotateVersionRowVersionIfNeeded(version);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return MapTerm(term);
        }, cancellationToken);
    }

    public async Task<ContractTemplateTermResponse> UpdateTermAsync(
        int versionId,
        int termId,
        UpdateContractTemplateTermRequest request,
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await EnsureAdminOfficerAsync(employeeId, cancellationToken);

        var values = NormalizeTerm(request.TermCode, request.TermTitle,
            request.TermTitleEn, request.TermContent, request.TermContentEn,
            request.DisplayOrder);
        var expectedVersionRowVersion = DecodeRowVersion(
            request.VersionRowVersion,
            nameof(request.VersionRowVersion));
        var expectedTermRowVersion = DecodeRowVersion(
            request.RowVersion,
            nameof(request.RowVersion));

        return await ExecuteInTransactionAsync(async () =>
        {
            await EnsureAdminOfficerAsync(employeeId, cancellationToken);
            var version = await GetVersionForMutationAsync(
                versionId,
                cancellationToken);
            EnsureDraft(version);
            EnsureRowVersionMatches(
                version.RowVersion,
                expectedVersionRowVersion,
                "Template version");
            SetOriginalRowVersion(version, expectedVersionRowVersion);

            var term = await _dbContext.TblContractTemplateTerms
                .SingleOrDefaultAsync(
                    candidate => candidate.TemplateTermId == termId
                        && candidate.TemplateVersionId == versionId,
                    cancellationToken);
            if (term is null)
            {
                throw new KeyNotFoundException("Không tìm thấy template term.");
            }

            EnsureRowVersionMatches(
                term.RowVersion,
                expectedTermRowVersion,
                "Template term");
            SetOriginalRowVersion(term, expectedTermRowVersion);
            await EnsureTermCodeAndDisplayOrderAreAvailableAsync(
                versionId,
                values.TermCode,
                values.DisplayOrder,
                termId,
                cancellationToken);
            ValidateTermKind(request.TermKind);
            await EnsureSinglePaymentTermAsync(versionId, request.TermKind, termId,
                cancellationToken);
            if (request.TermKind == ContractTermKind.General
                && term.TermKind == (byte)ContractTermKind.Payment
                && await _dbContext.TblContractTemplatePaymentMilestones.AnyAsync(
                    item => item.TemplateTermId == termId, cancellationToken))
            {
                throw new InvalidOperationException(
                    "Hãy xóa các đợt thanh toán trước khi chuyển thành điều khoản thường.");
            }

            term.TermCode = values.TermCode;
            term.TermTitle = values.TermTitle;
            term.TermTitleEn = values.TermTitleEn;
            term.TermContent = values.TermContent;
            term.TermContentEn = values.TermContentEn;
            term.TermKind = (byte)request.TermKind;
            term.IsNegotiable = request.IsNegotiable;
            term.DisplayOrder = values.DisplayOrder;
            term.UpdatedEmployeeId = employeeId;
            term.UpdatedDate = DateTime.UtcNow;
            TouchVersion(version, employeeId, term.UpdatedDate.Value);
            RotateTermRowVersionIfNeeded(term);
            RotateVersionRowVersionIfNeeded(version);

            await _dbContext.SaveChangesAsync(cancellationToken);
            return MapTerm(term);
        }, cancellationToken);
    }

    public async Task DeleteTermAsync(
        int versionId,
        int termId,
        DeleteContractTemplateTermRequest request,
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await EnsureAdminOfficerAsync(employeeId, cancellationToken);

        var expectedVersionRowVersion = DecodeRowVersion(
            request.VersionRowVersion,
            nameof(request.VersionRowVersion));
        var expectedTermRowVersion = DecodeRowVersion(
            request.RowVersion,
            nameof(request.RowVersion));

        await ExecuteInTransactionAsync(async () =>
        {
            await EnsureAdminOfficerAsync(employeeId, cancellationToken);
            var version = await GetVersionForMutationAsync(
                versionId,
                cancellationToken);
            EnsureDraft(version);
            EnsureRowVersionMatches(
                version.RowVersion,
                expectedVersionRowVersion,
                "Template version");
            SetOriginalRowVersion(version, expectedVersionRowVersion);

            var term = await _dbContext.TblContractTemplateTerms
                .SingleOrDefaultAsync(
                    candidate => candidate.TemplateTermId == termId
                        && candidate.TemplateVersionId == versionId,
                    cancellationToken);
            if (term is null)
            {
                throw new KeyNotFoundException("Không tìm thấy template term.");
            }

            EnsureRowVersionMatches(
                term.RowVersion,
                expectedTermRowVersion,
                "Template term");
            SetOriginalRowVersion(term, expectedTermRowVersion);
            _dbContext.TblContractTemplateTerms.Remove(term);
            TouchVersion(version, employeeId, DateTime.UtcNow);
            RotateVersionRowVersionIfNeeded(version);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }, cancellationToken);
    }

    public async Task<ContractTemplateVersionDetailResponse> ReorderTermsAsync(
        int versionId,
        ReorderContractTemplateTermsRequest request,
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await EnsureAdminOfficerAsync(employeeId, cancellationToken);

        var expectedVersionRowVersion = DecodeRowVersion(
            request.VersionRowVersion,
            nameof(request.VersionRowVersion));
        if (request.Terms is null)
        {
            throw new ArgumentException("Danh sách term không được null.");
        }

        foreach (var item in request.Terms)
        {
            DecodeRowVersion(item.RowVersion, nameof(item.RowVersion));
            if (item.TermId <= 0 || item.DisplayOrder < 0)
            {
                throw new ArgumentException(
                    "TermId phải dương và DisplayOrder không được âm.");
            }
        }

        if (request.Terms.Select(item => item.TermId).Distinct().Count()
                != request.Terms.Count
            || request.Terms.Select(item => item.DisplayOrder).Distinct().Count()
                != request.Terms.Count)
        {
            throw new ArgumentException(
                "TermId và DisplayOrder phải duy nhất trong danh sách reorder.");
        }

        return await ExecuteInTransactionAsync(async () =>
        {
            await EnsureAdminOfficerAsync(employeeId, cancellationToken);
            var version = await GetVersionForMutationAsync(
                versionId,
                cancellationToken);
            EnsureDraft(version);
            EnsureRowVersionMatches(
                version.RowVersion,
                expectedVersionRowVersion,
                "Template version");
            SetOriginalRowVersion(version, expectedVersionRowVersion);

            var terms = await _dbContext.TblContractTemplateTerms
                .Where(term => term.TemplateVersionId == versionId)
                .ToListAsync(cancellationToken);
            var expectedIds = terms.Select(term => term.TemplateTermId)
                .ToHashSet();
            var actualIds = request.Terms.Select(item => item.TermId)
                .ToHashSet();
            if (!expectedIds.SetEquals(actualIds))
            {
                throw new ArgumentException(
                    "Danh sách reorder phải chứa đúng toàn bộ term hiện có.");
            }

            var requestsById = request.Terms.ToDictionary(item => item.TermId);
            foreach (var term in terms)
            {
                var item = requestsById[term.TemplateTermId];
                var expectedTermRowVersion = DecodeRowVersion(
                    item.RowVersion,
                    nameof(item.RowVersion));
                EnsureRowVersionMatches(
                    term.RowVersion,
                    expectedTermRowVersion,
                    "Template term");
                SetOriginalRowVersion(term, expectedTermRowVersion);
                term.DisplayOrder = item.DisplayOrder;
                RotateTermRowVersionIfNeeded(term);
            }

            TouchVersion(version, employeeId, DateTime.UtcNow);
            RotateVersionRowVersionIfNeeded(version);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return await LoadVersionDetailAsync(versionId, cancellationToken);
        }, cancellationToken);
    }

    public async Task<ContractTemplatePaymentMilestoneResponse> AddPaymentMilestoneAsync(
        int versionId, int termId,
        CreateContractTemplatePaymentMilestoneRequest request, int employeeId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await EnsureAdminOfficerAsync(employeeId, cancellationToken);
        var values = NormalizePaymentMilestone(request);
        var versionRowVersion = DecodeRowVersion(request.VersionRowVersion,
            nameof(request.VersionRowVersion));
        return await ExecuteInTransactionAsync(async () =>
        {
            var version = await GetVersionForMutationAsync(versionId, cancellationToken);
            EnsureDraft(version);
            EnsureRowVersionMatches(version.RowVersion, versionRowVersion, "Template version");
            SetOriginalRowVersion(version, versionRowVersion);
            await EnsurePaymentTermAsync(versionId, termId, cancellationToken);
            await EnsurePaymentMilestoneUniqueAsync(versionId, termId,
                values.Code, values.Order, null, cancellationToken);
            if (values.Anchor == PaymentDueAnchor.PreviousMilestonePaid
                && !await _dbContext.TblContractTemplatePaymentMilestones.AnyAsync(
                    item => item.TemplateTermId == termId
                        && item.DisplayOrder < values.Order, cancellationToken))
                throw new ArgumentException("Đợt đầu tiên không thể phụ thuộc đợt trước.");

            var now = DateTime.UtcNow;
            var entity = new TblContractTemplatePaymentMilestone
            {
                TemplateVersionId = versionId, TemplateTermId = termId,
                MilestoneCode = values.Code, TitleVi = values.TitleVi,
                TitleEn = values.TitleEn, PaymentPercent = values.Percent,
                DueAnchor = (byte)values.Anchor, DueOffsetDays = values.Offset,
                DayCountMode = (byte)values.DayCount, ConditionVi = values.ConditionVi,
                ConditionEn = values.ConditionEn, DisplayOrder = values.Order,
                CreatedEmployeeId = employeeId, CreatedDate = now
            };
            SetSyntheticRowVersionIfNeeded(entity);
            _dbContext.TblContractTemplatePaymentMilestones.Add(entity);
            TouchVersion(version, employeeId, now);
            RotateVersionRowVersionIfNeeded(version);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return MapPaymentMilestone(entity);
        }, cancellationToken);
    }

    public async Task<ContractTemplatePaymentMilestoneResponse> UpdatePaymentMilestoneAsync(
        int versionId, int termId, int milestoneId,
        UpdateContractTemplatePaymentMilestoneRequest request, int employeeId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await EnsureAdminOfficerAsync(employeeId, cancellationToken);
        var values = NormalizePaymentMilestone(request);
        var versionRv = DecodeRowVersion(request.VersionRowVersion, nameof(request.VersionRowVersion));
        var rowRv = DecodeRowVersion(request.RowVersion, nameof(request.RowVersion));
        return await ExecuteInTransactionAsync(async () =>
        {
            var version = await GetVersionForMutationAsync(versionId, cancellationToken);
            EnsureDraft(version);
            EnsureRowVersionMatches(version.RowVersion, versionRv, "Template version");
            SetOriginalRowVersion(version, versionRv);
            await EnsurePaymentTermAsync(versionId, termId, cancellationToken);
            var entity = await _dbContext.TblContractTemplatePaymentMilestones.SingleOrDefaultAsync(
                item => item.TemplatePaymentMilestoneId == milestoneId
                    && item.TemplateVersionId == versionId && item.TemplateTermId == termId,
                cancellationToken) ?? throw new KeyNotFoundException("Không tìm thấy đợt thanh toán.");
            EnsureRowVersionMatches(entity.RowVersion, rowRv, "Đợt thanh toán");
            SetOriginalRowVersion(entity, rowRv);
            await EnsurePaymentMilestoneUniqueAsync(versionId, termId,
                values.Code, values.Order, milestoneId, cancellationToken);
            var firstOther = await _dbContext.TblContractTemplatePaymentMilestones
                .Where(item => item.TemplateTermId == termId
                    && item.TemplatePaymentMilestoneId != milestoneId)
                .OrderBy(item => item.DisplayOrder)
                .ThenBy(item => item.TemplatePaymentMilestoneId)
                .FirstOrDefaultAsync(cancellationToken);
            var updatedRowBecomesFirst = firstOther is null
                || values.Order < firstOther.DisplayOrder;
            var resultingFirstAnchor = updatedRowBecomesFirst
                ? values.Anchor
                : (PaymentDueAnchor)firstOther!.DueAnchor;
            if (resultingFirstAnchor == PaymentDueAnchor.PreviousMilestonePaid)
                throw new ArgumentException("Đợt đầu tiên không thể phụ thuộc đợt trước.");

            entity.MilestoneCode = values.Code; entity.TitleVi = values.TitleVi;
            entity.TitleEn = values.TitleEn; entity.PaymentPercent = values.Percent;
            entity.DueAnchor = (byte)values.Anchor; entity.DueOffsetDays = values.Offset;
            entity.DayCountMode = (byte)values.DayCount; entity.ConditionVi = values.ConditionVi;
            entity.ConditionEn = values.ConditionEn; entity.DisplayOrder = values.Order;
            entity.UpdatedEmployeeId = employeeId; entity.UpdatedDate = DateTime.UtcNow;
            SetSyntheticRowVersionIfNeeded(entity);
            TouchVersion(version, employeeId, entity.UpdatedDate.Value);
            RotateVersionRowVersionIfNeeded(version);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return MapPaymentMilestone(entity);
        }, cancellationToken);
    }

    public async Task DeletePaymentMilestoneAsync(
        int versionId, int termId, int milestoneId,
        DeleteContractTemplatePaymentMilestoneRequest request, int employeeId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await EnsureAdminOfficerAsync(employeeId, cancellationToken);
        var versionRv = DecodeRowVersion(request.VersionRowVersion, nameof(request.VersionRowVersion));
        var rowRv = DecodeRowVersion(request.RowVersion, nameof(request.RowVersion));
        await ExecuteInTransactionAsync(async () =>
        {
            var version = await GetVersionForMutationAsync(versionId, cancellationToken);
            EnsureDraft(version);
            EnsureRowVersionMatches(version.RowVersion, versionRv, "Template version");
            SetOriginalRowVersion(version, versionRv);
            await EnsurePaymentTermAsync(versionId, termId, cancellationToken);
            var entity = await _dbContext.TblContractTemplatePaymentMilestones.SingleOrDefaultAsync(
                item => item.TemplatePaymentMilestoneId == milestoneId
                    && item.TemplateTermId == termId && item.TemplateVersionId == versionId,
                cancellationToken) ?? throw new KeyNotFoundException("Không tìm thấy đợt thanh toán.");
            EnsureRowVersionMatches(entity.RowVersion, rowRv, "Đợt thanh toán");
            SetOriginalRowVersion(entity, rowRv);
            var nextFirst = await _dbContext.TblContractTemplatePaymentMilestones
                .Where(item => item.TemplateTermId == termId
                    && item.TemplatePaymentMilestoneId != milestoneId)
                .OrderBy(item => item.DisplayOrder)
                .ThenBy(item => item.TemplatePaymentMilestoneId)
                .FirstOrDefaultAsync(cancellationToken);
            if (nextFirst?.DueAnchor == (byte)PaymentDueAnchor.PreviousMilestonePaid)
                throw new InvalidOperationException(
                    "Không thể xóa vì đợt kế tiếp đang phụ thuộc vào đợt trước.");
            _dbContext.TblContractTemplatePaymentMilestones.Remove(entity);
            TouchVersion(version, employeeId, DateTime.UtcNow);
            RotateVersionRowVersionIfNeeded(version);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }, cancellationToken);
    }

    public async Task<ContractTemplateVersionDetailResponse> ReorderPaymentMilestonesAsync(
        int versionId, int termId, ReorderContractTemplatePaymentMilestonesRequest request,
        int employeeId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await EnsureAdminOfficerAsync(employeeId, cancellationToken);
        var versionRv = DecodeRowVersion(request.VersionRowVersion, nameof(request.VersionRowVersion));
        if (request.Milestones is null)
            throw new ArgumentException("Danh sách reorder không được null.");
        return await ExecuteInTransactionAsync(async () =>
        {
            var version = await GetVersionForMutationAsync(versionId, cancellationToken);
            EnsureDraft(version);
            EnsureRowVersionMatches(version.RowVersion, versionRv, "Template version");
            SetOriginalRowVersion(version, versionRv);
            await EnsurePaymentTermAsync(versionId, termId, cancellationToken);
            var rows = await _dbContext.TblContractTemplatePaymentMilestones
                .Where(item => item.TemplateTermId == termId && item.TemplateVersionId == versionId)
                .ToListAsync(cancellationToken);
            if (request.Milestones.Count != rows.Count
                || request.Milestones.Any(item => item.MilestoneId <= 0
                    || item.DisplayOrder < 0)
                || request.Milestones.Select(item => item.MilestoneId).Distinct().Count()
                    != rows.Count
                || !rows.Select(item => item.TemplatePaymentMilestoneId).ToHashSet()
                    .SetEquals(request.Milestones.Select(item => item.MilestoneId))
                || request.Milestones.Select(item => item.DisplayOrder).Distinct().Count() != rows.Count)
                throw new ArgumentException("Danh sách reorder phải chứa đúng toàn bộ đợt và thứ tự duy nhất.");
            var byId = request.Milestones.ToDictionary(item => item.MilestoneId);
            var requestedFirst = request.Milestones
                .OrderBy(item => item.DisplayOrder).ThenBy(item => item.MilestoneId)
                .FirstOrDefault();
            if (requestedFirst is not null
                && rows.Single(item => item.TemplatePaymentMilestoneId == requestedFirst.MilestoneId)
                    .DueAnchor == (byte)PaymentDueAnchor.PreviousMilestonePaid)
                throw new ArgumentException("Đợt đầu tiên không thể phụ thuộc đợt trước.");
            // Avoid transient unique-index collisions when two rows swap their
            // DisplayOrder values. Move every row into a temporary positive range,
            // flush, then assign the requested order inside the same transaction.
            var temporaryOrder = Math.Max(
                rows.Count == 0 ? 0 : rows.Max(item => item.DisplayOrder),
                request.Milestones.Count == 0 ? 0 : request.Milestones.Max(item => item.DisplayOrder))
                + 1_000_000;
            foreach (var row in rows)
            {
                var item = byId[row.TemplatePaymentMilestoneId];
                var rv = DecodeRowVersion(item.RowVersion, nameof(item.RowVersion));
                EnsureRowVersionMatches(row.RowVersion, rv, "Đợt thanh toán");
                SetOriginalRowVersion(row, rv);
                row.DisplayOrder = temporaryOrder++;
                SetSyntheticRowVersionIfNeeded(row);
            }
            await _dbContext.SaveChangesAsync(cancellationToken);
            foreach (var row in rows)
            {
                row.DisplayOrder = byId[row.TemplatePaymentMilestoneId].DisplayOrder;
                SetSyntheticRowVersionIfNeeded(row);
            }
            TouchVersion(version, employeeId, DateTime.UtcNow);
            RotateVersionRowVersionIfNeeded(version);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return await LoadVersionDetailAsync(versionId, cancellationToken);
        }, cancellationToken);
    }

    public async Task<ContractTemplateLegalBasisResponse> AddLegalBasisAsync(
        int versionId, CreateContractTemplateLegalBasisRequest request,
        int employeeId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await EnsureAdminOfficerAsync(employeeId, cancellationToken);
        var values = NormalizeLegalBasis(request.BasisCode, request.ContentVi,
            request.ContentEn, request.DisplayOrder);
        var expectedVersionRowVersion = DecodeRowVersion(
            request.VersionRowVersion, nameof(request.VersionRowVersion));

        return await ExecuteInTransactionAsync(async () =>
        {
            await EnsureAdminOfficerAsync(employeeId, cancellationToken);
            var version = await GetVersionForMutationAsync(versionId, cancellationToken);
            EnsureDraft(version);
            EnsureRowVersionMatches(version.RowVersion, expectedVersionRowVersion,
                "Template version");
            SetOriginalRowVersion(version, expectedVersionRowVersion);
            await EnsureLegalBasisCodeAndOrderAreAvailableAsync(versionId,
                values.BasisCode, values.DisplayOrder, null, cancellationToken);

            var now = DateTime.UtcNow;
            var basis = new TblContractTemplateLegalBasis
            {
                TemplateVersionId = versionId,
                BasisCode = values.BasisCode,
                ContentVi = values.ContentVi,
                ContentEn = values.ContentEn,
                DisplayOrder = values.DisplayOrder,
                CreatedEmployeeId = employeeId,
                CreatedDate = now
            };
            SetSyntheticRowVersionIfNeeded(basis);
            _dbContext.TblContractTemplateLegalBases.Add(basis);
            TouchVersion(version, employeeId, now);
            RotateVersionRowVersionIfNeeded(version);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return MapLegalBasis(basis);
        }, cancellationToken);
    }

    public async Task<ContractTemplateLegalBasisResponse> UpdateLegalBasisAsync(
        int versionId, int legalBasisId,
        UpdateContractTemplateLegalBasisRequest request, int employeeId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await EnsureAdminOfficerAsync(employeeId, cancellationToken);
        var values = NormalizeLegalBasis(request.BasisCode, request.ContentVi,
            request.ContentEn, request.DisplayOrder);
        var expectedVersionRowVersion = DecodeRowVersion(
            request.VersionRowVersion, nameof(request.VersionRowVersion));
        var expectedRowVersion = DecodeRowVersion(request.RowVersion,
            nameof(request.RowVersion));

        return await ExecuteInTransactionAsync(async () =>
        {
            await EnsureAdminOfficerAsync(employeeId, cancellationToken);
            var version = await GetVersionForMutationAsync(versionId, cancellationToken);
            EnsureDraft(version);
            EnsureRowVersionMatches(version.RowVersion, expectedVersionRowVersion,
                "Template version");
            SetOriginalRowVersion(version, expectedVersionRowVersion);
            var basis = await _dbContext.TblContractTemplateLegalBases
                .SingleOrDefaultAsync(item => item.TemplateLegalBasisId == legalBasisId
                    && item.TemplateVersionId == versionId, cancellationToken)
                ?? throw new KeyNotFoundException("Không tìm thấy căn cứ hợp đồng.");
            EnsureRowVersionMatches(basis.RowVersion, expectedRowVersion,
                "Căn cứ hợp đồng");
            SetOriginalRowVersion(basis, expectedRowVersion);
            await EnsureLegalBasisCodeAndOrderAreAvailableAsync(versionId,
                values.BasisCode, values.DisplayOrder, legalBasisId, cancellationToken);

            basis.BasisCode = values.BasisCode;
            basis.ContentVi = values.ContentVi;
            basis.ContentEn = values.ContentEn;
            basis.DisplayOrder = values.DisplayOrder;
            basis.UpdatedEmployeeId = employeeId;
            basis.UpdatedDate = DateTime.UtcNow;
            RotateLegalBasisRowVersionIfNeeded(basis);
            TouchVersion(version, employeeId, basis.UpdatedDate.Value);
            RotateVersionRowVersionIfNeeded(version);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return MapLegalBasis(basis);
        }, cancellationToken);
    }

    public async Task DeleteLegalBasisAsync(
        int versionId, int legalBasisId,
        DeleteContractTemplateLegalBasisRequest request, int employeeId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await EnsureAdminOfficerAsync(employeeId, cancellationToken);
        var expectedVersionRowVersion = DecodeRowVersion(
            request.VersionRowVersion, nameof(request.VersionRowVersion));
        var expectedRowVersion = DecodeRowVersion(request.RowVersion,
            nameof(request.RowVersion));

        await ExecuteInTransactionAsync(async () =>
        {
            await EnsureAdminOfficerAsync(employeeId, cancellationToken);
            var version = await GetVersionForMutationAsync(versionId, cancellationToken);
            EnsureDraft(version);
            EnsureRowVersionMatches(version.RowVersion, expectedVersionRowVersion,
                "Template version");
            SetOriginalRowVersion(version, expectedVersionRowVersion);
            var basis = await _dbContext.TblContractTemplateLegalBases
                .SingleOrDefaultAsync(item => item.TemplateLegalBasisId == legalBasisId
                    && item.TemplateVersionId == versionId, cancellationToken)
                ?? throw new KeyNotFoundException("Không tìm thấy căn cứ hợp đồng.");
            EnsureRowVersionMatches(basis.RowVersion, expectedRowVersion,
                "Căn cứ hợp đồng");
            SetOriginalRowVersion(basis, expectedRowVersion);
            _dbContext.TblContractTemplateLegalBases.Remove(basis);
            TouchVersion(version, employeeId, DateTime.UtcNow);
            RotateVersionRowVersionIfNeeded(version);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }, cancellationToken);
    }

    public async Task<ContractTemplateVersionDetailResponse> ReorderLegalBasesAsync(
        int versionId, ReorderContractTemplateLegalBasesRequest request,
        int employeeId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await EnsureAdminOfficerAsync(employeeId, cancellationToken);
        var expectedVersionRowVersion = DecodeRowVersion(
            request.VersionRowVersion, nameof(request.VersionRowVersion));
        if (request.LegalBases is null
            || request.LegalBases.Any(item => item.LegalBasisId <= 0 || item.DisplayOrder < 0)
            || request.LegalBases.Select(item => item.LegalBasisId).Distinct().Count() != request.LegalBases.Count
            || request.LegalBases.Select(item => item.DisplayOrder).Distinct().Count() != request.LegalBases.Count)
        {
            throw new ArgumentException(
                "Danh sách căn cứ và DisplayOrder phải hợp lệ, duy nhất.");
        }

        return await ExecuteInTransactionAsync(async () =>
        {
            await EnsureAdminOfficerAsync(employeeId, cancellationToken);
            var version = await GetVersionForMutationAsync(versionId, cancellationToken);
            EnsureDraft(version);
            EnsureRowVersionMatches(version.RowVersion, expectedVersionRowVersion,
                "Template version");
            SetOriginalRowVersion(version, expectedVersionRowVersion);
            var bases = await _dbContext.TblContractTemplateLegalBases
                .Where(item => item.TemplateVersionId == versionId)
                .ToListAsync(cancellationToken);
            if (!bases.Select(item => item.TemplateLegalBasisId).ToHashSet()
                .SetEquals(request.LegalBases.Select(item => item.LegalBasisId)))
            {
                throw new ArgumentException(
                    "Danh sách reorder phải chứa đúng toàn bộ căn cứ hiện có.");
            }

            var requestsById = request.LegalBases.ToDictionary(item => item.LegalBasisId);
            foreach (var basis in bases)
            {
                var item = requestsById[basis.TemplateLegalBasisId];
                var expectedRowVersion = DecodeRowVersion(item.RowVersion,
                    nameof(item.RowVersion));
                EnsureRowVersionMatches(basis.RowVersion, expectedRowVersion,
                    "Căn cứ hợp đồng");
                SetOriginalRowVersion(basis, expectedRowVersion);
                basis.DisplayOrder = item.DisplayOrder;
                RotateLegalBasisRowVersionIfNeeded(basis);
            }
            TouchVersion(version, employeeId, DateTime.UtcNow);
            RotateVersionRowVersionIfNeeded(version);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return await LoadVersionDetailAsync(versionId, cancellationToken);
        }, cancellationToken);
    }

    private async Task EnsureAdminOfficerAsync(
        int employeeId,
        CancellationToken cancellationToken)
    {
        if (employeeId <= 0)
        {
            throw new UnauthorizedAccessException(
                "Không xác định được nhân viên đang đăng nhập.");
        }

        var actor = await _dbContext.TblEmployees
            .AsNoTracking()
            .SingleOrDefaultAsync(
                employee => employee.EmployeeId == employeeId
                    && employee.Status == ActiveEmployeeStatus,
                cancellationToken);
        if (actor is null
            || actor.EmployeeType != (byte)EmployeeType.AdminOfficer)
        {
            throw new UnauthorizedAccessException(
                "Chỉ AdminOfficer active được quản trị template.");
        }
    }

    private async Task<TblContractTemplate> GetTemplateAsync(
        int templateId,
        CancellationToken cancellationToken)
    {
        if (templateId <= 0)
        {
            throw new ArgumentException("TemplateId phải dương.");
        }

        var template = await _dbContext.TblContractTemplates
            .SingleOrDefaultAsync(
                candidate => candidate.TemplateId == templateId
                    && candidate.DocumentType
                        == (byte)TemplateDocumentType.SoftwareSupplyContract,
                cancellationToken);
        return template
            ?? throw new KeyNotFoundException("Không tìm thấy template.");
    }

    private async Task<TblContractTemplate> GetTemplateForPreviewAsync(
        int templateId,
        CancellationToken cancellationToken)
    {
        var template = await _dbContext.TblContractTemplates
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.TemplateId == templateId
                    && candidate.DocumentType
                        == (byte)TemplateDocumentType.SoftwareSupplyContract,
                cancellationToken);
        return template
            ?? throw new KeyNotFoundException("Không tìm thấy template.");
    }

    private async Task<TblContractTemplateVersion> GetVersionForMutationAsync(
        int versionId,
        CancellationToken cancellationToken)
    {
        if (versionId <= 0)
        {
            throw new ArgumentException("TemplateVersionId phải dương.");
        }

        var version = await _dbContext.TblContractTemplateVersions
            .SingleOrDefaultAsync(
                candidate => candidate.TemplateVersionId == versionId
                    && _dbContext.TblContractTemplates.Any(template =>
                        template.TemplateId == candidate.TemplateId
                        && template.DocumentType
                            == (byte)TemplateDocumentType.SoftwareSupplyContract),
                cancellationToken);
        return version
            ?? throw new KeyNotFoundException("Không tìm thấy template version.");
    }

    private async Task<TblContractTemplateVersion>
        GetVersionForUploadPreflightAsync(
            int versionId,
            CancellationToken cancellationToken)
    {
        if (versionId <= 0)
        {
            throw new ArgumentException("TemplateVersionId phải dương.");
        }

        // Do not track preflight data: the transaction below must read a fresh
        // Version before persisting a potentially expensive upload.
        var version = await _dbContext.TblContractTemplateVersions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.TemplateVersionId == versionId
                    && _dbContext.TblContractTemplates.Any(template =>
                        template.TemplateId == candidate.TemplateId
                        && template.DocumentType
                            == (byte)TemplateDocumentType.SoftwareSupplyContract),
                cancellationToken);
        return version
            ?? throw new KeyNotFoundException("Không tìm thấy template version.");
    }

    private async Task ReplaceFieldSnapshotAsync(
        int versionId,
        ContractTemplateDocumentValidationResult validation,
        int employeeId,
        CancellationToken cancellationToken)
    {
        var recognized = new HashSet<string>(validation.RecognizedPlaceholderKeys,
            StringComparer.Ordinal);
        var currentCatalog = await _placeholderCatalog.GetAsync(cancellationToken: cancellationToken);
        if (validation.CatalogRevision is not null && validation.CatalogRevision != ContractPlaceholderCatalog.Fingerprint(currentCatalog))
            throw new DbUpdateConcurrencyException("Catalog placeholder đã thay đổi trong khi upload. Hãy thử lại.");
        var definitions = validation.Definitions ?? currentCatalog.Where(x => recognized.Contains(x.Key)).ToArray();

        if (IsInMemoryProvider())
        {
            // Keep InMemory tests rollback-like: no field mutation is committed
            // until Version and audit have been staged successfully.
            var existing = await _dbContext.TblContractTemplateFields
                .Where(field => field.TemplateVersionId == versionId)
                .ToListAsync(cancellationToken);
            _dbContext.TblContractTemplateFields.RemoveRange(existing);
        }
        else
        {
            // SQL executes this delete in the surrounding transaction before the
            // replacement inserts, avoiding a transient unique-key collision.
            await _dbContext.TblContractTemplateFields
                .Where(field => field.TemplateVersionId == versionId)
                .ExecuteDeleteAsync(cancellationToken);
        }

        var now = DateTime.UtcNow;
        var displayOrder = 0;
        foreach (var definition in definitions)
        {
            if (!recognized.Contains(definition.Key))
            {
                continue;
            }

            var field = new TblContractTemplateField
            {
                TemplateVersionId = versionId,
                PlaceholderKey = definition.Key,
                FieldLabel = definition.Label,
                DataSource = definition.DataSource,
                SourceFieldKey = definition.SourceFieldKey,
                DataKind = (byte)definition.DataKind,
                Multiplicity = (byte)definition.Multiplicity,
                IsSystem = definition.IsSystem,
                DefinitionRowVersion = definition.RowVersion is null ? null : Convert.FromBase64String(definition.RowVersion),
                DefaultValue = definition.DefaultValue,
                FormatString = definition.FormatString,
                IsRequired = definition.IsRequired,
                DisplayOrder = displayOrder++,
                CreatedEmployeeId = employeeId,
                CreatedDate = now
            };
            _dbContext.TblContractTemplateFields.Add(field);
        }
    }

    private void StageDocumentAudits(
        TblContractTemplateVersion version,
        int employeeId,
        bool replacesExistingDocument,
        TblFileStorage? oldFile,
        FileStorageResponse uploadedFile,
        ContractTemplateDocumentValidationResult validation,
        DateTime occurredAt)
    {
        var writer = _templateAuditWriter
            ?? throw new InvalidOperationException(
                "Template audit writer chưa được cấu hình.");
        var oldValues = BuildAuditValues(
            oldFile?.FileId,
            oldFile?.FileType,
            oldFile?.FileSize,
            "Unchanged",
            recognizedPlaceholderCount: null);
        var newValues = BuildAuditValues(
            uploadedFile.FileId,
            validation.FileExtension,
            validation.FileSizeBytes,
            validation.IsCatalogValid ? "Valid" : "Invalid",
            validation.RecognizedPlaceholderKeys.Count);
        var requests = new List<ContractTemplateAuditWriteRequest>
        {
            new(
                version.TemplateId,
                version.TemplateVersionId,
                employeeId,
                replacesExistingDocument
                    ? ContractTemplateAuditActionTypes.DocumentReplaced
                    : ContractTemplateAuditActionTypes.DocumentUploaded,
                ContractTemplateAuditResults.Succeeded,
                occurredAt,
                oldValues,
                newValues)
        };

        if (!validation.IsCatalogValid)
        {
            requests.Add(new ContractTemplateAuditWriteRequest(
                version.TemplateId,
                version.TemplateVersionId,
                employeeId,
                ContractTemplateAuditActionTypes.ValidationInvalid,
                ContractTemplateAuditResults.Invalid,
                occurredAt,
                oldValues,
                newValues,
                validation.FailureCode));
        }

        writer.StageAudits(requests);
    }

    private async Task RecordTechnicalRejectionAsync(
        TblContractTemplateVersion version,
        int employeeId,
        ContractTemplateDocumentValidationResult validation,
        CancellationToken cancellationToken)
    {
        if (_templateAuditWriter is null)
        {
            return;
        }

        _templateAuditWriter.StageAudits(
        [
            new ContractTemplateAuditWriteRequest(
                version.TemplateId,
                version.TemplateVersionId,
                employeeId,
                ContractTemplateAuditActionTypes.ValidationRejected,
                ContractTemplateAuditResults.Rejected,
                DateTime.UtcNow,
                NewValues: BuildAuditValues(
                    documentFileId: null,
                    extension: validation.FileExtension,
                    sizeBytes: validation.FileSizeBytes,
                    validationStatus: "Unchanged",
                    recognizedPlaceholderCount: null),
                FailureCode: validation.FailureCode)
        ]);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task RecordConcurrencyConflictAsync(
        TblContractTemplateVersion version,
        int employeeId,
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (_templateAuditWriter is null)
        {
            return;
        }

        try
        {
            _templateAuditWriter.StageAudits(
            [
                new ContractTemplateAuditWriteRequest(
                    version.TemplateId,
                    version.TemplateVersionId,
                    employeeId,
                    ContractTemplateAuditActionTypes.ConcurrencyConflict,
                    ContractTemplateAuditResults.Conflict,
                    DateTime.UtcNow,
                    NewValues: BuildAuditValues(
                        documentFileId: null,
                        extension: GetSafeAuditExtension(file?.FileName),
                        sizeBytes: Math.Max(file?.Length ?? 0, 0),
                        validationStatus: "Unchanged",
                        recognizedPlaceholderCount: null),
                    FailureCode: "StaleVersionRowVersion")
            ]);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            // A failure to append observability must not turn an established
            // optimistic-concurrency conflict into a misleading server error.
            _logger?.LogError(exception,
                "Failed to append Template DOCX concurrency audit for Version {VersionId}.",
                version.TemplateVersionId);
        }
    }

    private void StagePreviewGeneratedAudit(
        TblContractTemplateVersion version,
        int employeeId,
        int? previousPreviewFileId,
        FileStorageResponse previewFile,
        DateTime occurredAt)
    {
        var writer = _templateAuditWriter
            ?? throw new InvalidOperationException(
                "Template audit writer chưa được cấu hình.");
        writer.StageAudits(
        [
            new ContractTemplateAuditWriteRequest(
                version.TemplateId,
                version.TemplateVersionId,
                employeeId,
                ContractTemplateAuditActionTypes.PreviewGenerated,
                ContractTemplateAuditResults.Succeeded,
                occurredAt,
                PreviousValues: BuildPreviewAuditValues(
                    previousPreviewFileId,
                    sizeBytes: null,
                    status: previousPreviewFileId is > 0 ? "Stale" : "Unchanged"),
                NewValues: BuildPreviewAuditValues(
                    previewFile.FileId,
                    Math.Max(previewFile.FileSize ?? 0, 0),
                    "Current"))
        ]);
    }

    private async Task RecordPreviewRejectionAsync(
        TblContractTemplateVersion version,
        int employeeId,
        string failureCode,
        CancellationToken cancellationToken)
    {
        if (_templateAuditWriter is null)
        {
            return;
        }

        _templateAuditWriter.StageAudits(
        [
            new ContractTemplateAuditWriteRequest(
                version.TemplateId,
                version.TemplateVersionId,
                employeeId,
                ContractTemplateAuditActionTypes.PreviewRejected,
                ContractTemplateAuditResults.Rejected,
                DateTime.UtcNow,
                NewValues: BuildPreviewAuditValues(
                    previewFileId: null,
                    sizeBytes: null,
                    status: "Rejected"),
                FailureCode: failureCode)
        ]);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task RecordPreviewConcurrencyConflictAsync(
        TblContractTemplateVersion version,
        int employeeId,
        CancellationToken cancellationToken)
    {
        if (_templateAuditWriter is null)
        {
            return;
        }

        try
        {
            _templateAuditWriter.StageAudits(
            [
                new ContractTemplateAuditWriteRequest(
                    version.TemplateId,
                    version.TemplateVersionId,
                    employeeId,
                    ContractTemplateAuditActionTypes.PreviewConcurrencyConflict,
                    ContractTemplateAuditResults.Conflict,
                    DateTime.UtcNow,
                    NewValues: BuildPreviewAuditValues(
                        previewFileId: null,
                        sizeBytes: null,
                        status: "Unchanged"),
                    FailureCode: "StaleVersionRowVersion")
            ]);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            _logger?.LogError(exception,
                "Failed to append Template preview concurrency audit for Version {VersionId}.",
                version.TemplateVersionId);
        }
    }

    private void StagePublishAudits(
        TblContractTemplateVersion published,
        TblContractTemplateVersion? retired,
        int employeeId,
        FileStorageResponse pdfFile,
        DateTime occurredAt)
    {
        var writer = _templateAuditWriter ?? throw new InvalidOperationException(
            "Template audit writer chưa được cấu hình.");
        var requests = new List<ContractTemplateAuditWriteRequest>();
        if (retired is not null)
        {
            requests.Add(new ContractTemplateAuditWriteRequest(
                retired.TemplateId, retired.TemplateVersionId, employeeId,
                ContractTemplateAuditActionTypes.TemplateVersionRetired,
                ContractTemplateAuditResults.Succeeded, occurredAt,
                PreviousValues: BuildPublishAuditValues(
                    retired.PublishedPreviewPdfFileId, null, "Published"),
                NewValues: BuildPublishAuditValues(
                    retired.PublishedPreviewPdfFileId, null, "Retired")));
        }

        requests.Add(new ContractTemplateAuditWriteRequest(
            published.TemplateId, published.TemplateVersionId, employeeId,
            ContractTemplateAuditActionTypes.TemplateVersionPublished,
            ContractTemplateAuditResults.Succeeded, occurredAt,
            PreviousValues: BuildPublishAuditValues(null, null, "Draft"),
            NewValues: BuildPublishAuditValues(pdfFile.FileId,
                Math.Max(pdfFile.FileSize ?? 0, 0), "Published")));
        writer.StageAudits(requests);
    }

    private void StageRetiredAudit(
        TblContractTemplateVersion version,
        int employeeId,
        DateTime occurredAt)
    {
        var writer = _templateAuditWriter ?? throw new InvalidOperationException(
            "Template audit writer chưa được cấu hình.");
        writer.StageAudits(
        [
            new ContractTemplateAuditWriteRequest(
                version.TemplateId, version.TemplateVersionId, employeeId,
                ContractTemplateAuditActionTypes.TemplateVersionRetired,
                ContractTemplateAuditResults.Succeeded, occurredAt,
                PreviousValues: BuildPublishAuditValues(
                    version.PublishedPreviewPdfFileId, null, "Published"),
                NewValues: BuildPublishAuditValues(
                    version.PublishedPreviewPdfFileId, null, "Retired"))
        ]);
    }

    private async Task RecordPdfRenderFailureAsync(
        TblContractTemplateVersion version,
        int employeeId,
        string failureCode,
        CancellationToken cancellationToken)
    {
        if (_templateAuditWriter is null)
        {
            return;
        }

        try
        {
            _templateAuditWriter.StageAudits(
            [
                new ContractTemplateAuditWriteRequest(
                    version.TemplateId, version.TemplateVersionId, employeeId,
                    ContractTemplateAuditActionTypes.PdfRenderFailed,
                    ContractTemplateAuditResults.Rejected, DateTime.UtcNow,
                    NewValues: BuildPublishAuditValues(null, null, "Draft"),
                    FailureCode: failureCode)
            ]);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            _logger?.LogError(exception,
                "Failed to append PDF render audit for Template Version {VersionId}.",
                version.TemplateVersionId);
        }
    }

    private async Task RecordPublishConcurrencyConflictAsync(
        TblContractTemplateVersion version,
        int employeeId,
        CancellationToken cancellationToken)
    {
        if (_templateAuditWriter is null)
        {
            return;
        }

        try
        {
            _templateAuditWriter.StageAudits(
            [
                new ContractTemplateAuditWriteRequest(
                    version.TemplateId, version.TemplateVersionId, employeeId,
                    ContractTemplateAuditActionTypes.PublishConcurrencyConflict,
                    ContractTemplateAuditResults.Conflict, DateTime.UtcNow,
                    NewValues: BuildPublishAuditValues(null, null, "Unchanged"),
                    FailureCode: "StaleVersionRowVersion")
            ]);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            _logger?.LogError(exception,
                "Failed to append publish concurrency audit for Template Version {VersionId}.",
                version.TemplateVersionId);
        }
    }

    private static IReadOnlyDictionary<string, object?> BuildPreviewAuditValues(
        int? previewFileId,
        long? sizeBytes,
        string status)
    {
        var values = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["PreviewFileId"] = previewFileId,
            ["PreviewStatus"] = status
        };
        if (sizeBytes.HasValue)
        {
            values["PreviewSizeBytes"] = Math.Max(sizeBytes.Value, 0);
        }

        return values;
    }

    private static IReadOnlyDictionary<string, object?> BuildPublishAuditValues(
        int? pdfFileId,
        long? sizeBytes,
        string status)
    {
        var values = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["PublishedPreviewPdfFileId"] = pdfFileId,
            ["PublishStatus"] = status
        };
        if (sizeBytes.HasValue)
        {
            values["PublishedPreviewPdfSizeBytes"] =
                Math.Max(sizeBytes.Value, 0);
        }

        return values;
    }

    private async Task<byte[]> DownloadAndVerifySourceDocumentAsync(
        TblContractTemplateVersion version,
        CancellationToken cancellationToken)
    {
        var artifact = await _fileStorageService!.DownloadAsync(
            version.DocumentFileId!.Value);
        if (artifact is null)
        {
            throw new ContractTemplatePreviewException(
                "PreviewSourceUnavailable",
                "DOCX template nguồn không còn khả dụng để tạo preview.");
        }

        await using var source = artifact.Value.Stream;
        await using var memory = new MemoryStream();
        await source.CopyToAsync(memory, cancellationToken);
        var bytes = memory.ToArray();
        var actualHash = Convert.ToHexString(SHA256.HashData(bytes))
            .ToLowerInvariant();
        if (!string.Equals(actualHash, version.DocumentHash,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ContractTemplatePreviewException(
                "PreviewSourceHashMismatch",
                "DOCX template nguồn không khớp hash đã validation.");
        }

        return bytes;
    }

    private async Task<bool> HasCurrentPreviewAsync(
        TblContractTemplateVersion version,
        string fingerprint,
        CancellationToken cancellationToken)
    {
        if (version.PreviewFileId is not > 0
            || !string.Equals(version.PreviewSourceHash, fingerprint,
                StringComparison.Ordinal))
        {
            return false;
        }

        var hasMetadata = await _dbContext.TblFileStorages
            .AsNoTracking()
            .AnyAsync(file => file.FileId == version.PreviewFileId.Value
                && file.ObjectType == ContractTemplatePreviewObjectType
                && file.ObjectId == version.TemplateVersionId,
                cancellationToken);
        if (!hasMetadata)
        {
            return false;
        }

        // Metadata alone is not an existing preview when its physical artifact
        // has disappeared. In that case POST renders a replacement instead of
        // falsely returning a broken current preview.
        var artifact = await _fileStorageService!.DownloadAsync(
            version.PreviewFileId.Value);
        if (artifact is null)
        {
            return false;
        }

        await using var stream = artifact.Value.Stream;
        return true;
    }

    private async Task<byte[]> DownloadCurrentPreviewBytesAsync(
        TblContractTemplateVersion version,
        string fingerprint,
        CancellationToken cancellationToken)
    {
        if (version.PreviewFileId is not > 0
            || !string.Equals(version.PreviewSourceHash, fingerprint,
                StringComparison.Ordinal))
        {
            throw new ContractTemplatePreviewException("PreviewStale",
                "DOCX preview hiện hành không còn khớp nguồn template.");
        }

        var owned = await _dbContext.TblFileStorages.AsNoTracking().AnyAsync(file =>
            file.FileId == version.PreviewFileId.Value
            && file.ObjectType == ContractTemplatePreviewObjectType
            && file.ObjectId == version.TemplateVersionId, cancellationToken);
        if (!owned)
        {
            throw new ContractTemplatePreviewException("PreviewArtifactUnavailable",
                "Artifact DOCX preview hiện hành không còn khả dụng.");
        }

        var artifact = await _fileStorageService!.DownloadAsync(
            version.PreviewFileId.Value);
        if (artifact is null)
        {
            throw new ContractTemplatePreviewException("PreviewArtifactUnavailable",
                "Artifact DOCX preview hiện hành không còn khả dụng.");
        }

        await using var source = artifact.Value.Stream;
        await using var memory = new MemoryStream();
        await source.CopyToAsync(memory, cancellationToken);
        return memory.ToArray();
    }

    private static void EnsurePreviewEligible(
        TblContractTemplateVersion version)
    {
        if (version.Status != (byte)TemplateVersionStatus.Draft
            || version.ValidationStatus != (byte)TemplateValidationStatus.Valid
            || version.DocumentFileId is not > 0
            || !IsSha256Hex(version.DocumentHash))
        {
            throw new ContractTemplatePreviewException(
                "PreviewPrerequisiteNotMet",
                "Preview chỉ hỗ trợ TemplateVersion Draft đã validation Valid và có DOCX nguồn hợp lệ.");
        }
    }

    private static void EnsurePreviewDownloadEligible(
        TblContractTemplateVersion version)
    {
        var canDownload = version.Status == (byte)TemplateVersionStatus.Draft
            || version.Status == (byte)TemplateVersionStatus.Published
            || version.Status == (byte)TemplateVersionStatus.Retired;
        if (!canDownload
            || version.ValidationStatus != (byte)TemplateValidationStatus.Valid
            || version.DocumentFileId is not > 0
            || !IsSha256Hex(version.DocumentHash))
        {
            throw new ContractTemplatePreviewException("PreviewPrerequisiteNotMet",
                "DOCX preview chỉ có thể tải khi TemplateVersion còn preview hợp lệ.");
        }
    }

    private static void EnsurePublishEligible(TblContractTemplateVersion version)
    {
        if (version.Status != (byte)TemplateVersionStatus.Draft
            || version.ValidationStatus != (byte)TemplateValidationStatus.Valid
            || version.DocumentFileId is not > 0
            || !IsSha256Hex(version.DocumentHash)
            || version.PreviewFileId is not > 0)
        {
            throw new ContractTemplatePreviewException("PublishPrerequisiteNotMet",
                "Publish yêu cầu Draft đã Valid, DOCX nguồn và DOCX preview hiện hành hợp lệ.");
        }
    }

    private static string CreatePreviewSourceHash(
        string documentHash,
        ContractLanguageMode languageMode,
        string? bindingHash = null,
        string? authoringDataHash = null)
    {
        var source = string.Join('|',
            documentHash.Trim().ToLowerInvariant(),
            // Versions published before binding snapshots retain their original preview fingerprint.
            bindingHash ?? "V2",
            authoringDataHash ?? "NO_AUTHORING_DATA",
            SoftwareSupplyPreviewDatasetV1.Version,
            bindingHash is null ? "V3" : ContractTemplatePreviewRenderer.FormatVersion,
            ((byte)languageMode).ToString(System.Globalization.CultureInfo.InvariantCulture));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)))
            .ToLowerInvariant();
    }

    private async Task ValidatePaymentTermsForPublishAsync(int versionId,
        CancellationToken cancellationToken)
    {
        var paymentTerms = await _dbContext.TblContractTemplateTerms.AsNoTracking()
            .Where(term => term.TemplateVersionId == versionId
                && term.TermKind == (byte)ContractTermKind.Payment)
            .Select(term => term.TemplateTermId).ToListAsync(cancellationToken);
        if (paymentTerms.Count > 1)
            throw new ContractTemplatePreviewException("PaymentTermInvalid",
                "Mỗi template version chỉ được có một điều khoản thanh toán.");
        if (paymentTerms.Count == 0) return;
        var milestones = await _dbContext.TblContractTemplatePaymentMilestones.AsNoTracking()
            .Where(item => item.TemplateTermId == paymentTerms[0]).OrderBy(item => item.DisplayOrder)
            .ToListAsync(cancellationToken);
        if (milestones.Count == 0 || milestones.Sum(item => item.PaymentPercent) != 100m)
            throw new ContractTemplatePreviewException("PaymentMilestonesInvalid",
                "Điều khoản thanh toán phải có ít nhất một đợt và tổng tỷ lệ đúng 100%.");
        if (milestones[0].DueAnchor == (byte)PaymentDueAnchor.PreviousMilestonePaid)
            throw new ContractTemplatePreviewException("PaymentMilestonesInvalid",
                "Đợt thanh toán đầu tiên không thể phụ thuộc đợt trước.");
    }

    private async Task<TemplatePreviewInput> LoadTemplatePreviewInputAsync(
        int versionId,
        CancellationToken cancellationToken)
    {
        var bases = await _dbContext.TblContractTemplateLegalBases
            .AsNoTracking()
            .Where(item => item.TemplateVersionId == versionId)
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.TemplateLegalBasisId)
            .Select(item => new
            {
                item.BasisCode,
                item.ContentVi,
                item.ContentEn,
                item.DisplayOrder
            })
            .ToListAsync(cancellationToken);

        var terms = await _dbContext.TblContractTemplateTerms.AsNoTracking()
            .Where(term => term.TemplateVersionId == versionId)
            .OrderBy(term => term.DisplayOrder).ThenBy(term => term.TemplateTermId)
            .ToListAsync(cancellationToken);
        var milestones = await _dbContext.TblContractTemplatePaymentMilestones.AsNoTracking()
            .Where(item => item.TemplateVersionId == versionId)
            .OrderBy(item => item.DisplayOrder).ThenBy(item => item.TemplatePaymentMilestoneId)
            .ToListAsync(cancellationToken);

        var basisCanonical = string.Join('\n', bases.Select(item => string.Join('|',
            EscapeCanonical(item.BasisCode),
            EscapeCanonical(item.ContentVi),
            EscapeCanonical(item.ContentEn),
            item.DisplayOrder.ToString(System.Globalization.CultureInfo.InvariantCulture))));
        var invariant = System.Globalization.CultureInfo.InvariantCulture;
        var paymentCanonical = string.Join('\n', terms.Select(term =>
        {
            var milestoneCanonical = string.Join('~', milestones
                .Where(item => item.TemplateTermId == term.TemplateTermId)
                .Select(item => string.Join(',',
                    EscapeCanonical(item.MilestoneCode),
                    EscapeCanonical(item.TitleVi), EscapeCanonical(item.TitleEn),
                    item.PaymentPercent.ToString(invariant),
                    item.DueAnchor.ToString(invariant),
                    item.DueOffsetDays.ToString(invariant),
                    item.DayCountMode.ToString(invariant), EscapeCanonical(item.ConditionVi),
                    EscapeCanonical(item.ConditionEn),
                    item.DisplayOrder.ToString(invariant))));
            return string.Join('|', EscapeCanonical(term.TermCode),
                term.TermKind.ToString(invariant), EscapeCanonical(term.TermTitle),
                EscapeCanonical(term.TermTitleEn), EscapeCanonical(term.TermContent),
                EscapeCanonical(term.TermContentEn), term.DisplayOrder.ToString(invariant),
                milestoneCanonical);
        }));
        var canonical = string.Join("\n", new[] { "LEGAL", basisCanonical, "TERMS", paymentCanonical });
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)))
            .ToLowerInvariant();
        var sampleTotal = SoftwareSupplyPreviewDatasetV1.Payments.Sum(item => item.Amount);
        return new TemplatePreviewInput(
            new ContractTemplateAuthoringPreviewData
            {
                LegalBases = bases.Select((item, index) =>
                    new ContractTemplateRenderLegalBasis(
                        index + 1, item.ContentVi, item.ContentEn)).ToList(),
                Terms = terms.Select((term, index) =>
                {
                    var termMilestones = milestones
                        .Where(item => item.TemplateTermId == term.TemplateTermId)
                        .ToList();
                    var renderMilestones = new List<ContractTemplateRenderPaymentMilestone>(
                        termMilestones.Count);
                    decimal allocated = 0m;
                    for (var milestoneIndex = 0;
                         milestoneIndex < termMilestones.Count;
                         milestoneIndex++)
                    {
                        var item = termMilestones[milestoneIndex];
                        var amount = milestoneIndex == termMilestones.Count - 1
                            ? sampleTotal - allocated
                            : Math.Round(sampleTotal * item.PaymentPercent / 100m,
                                0, MidpointRounding.AwayFromZero);
                        allocated += amount;
                        renderMilestones.Add(new ContractTemplateRenderPaymentMilestone(
                            milestoneIndex + 1, item.TitleVi, item.TitleEn,
                            item.PaymentPercent, amount,
                            (PaymentDueAnchor)item.DueAnchor, item.DueOffsetDays,
                            (PaymentDayCountMode)item.DayCountMode,
                            item.ConditionVi, item.ConditionEn));
                    }

                    return new ContractTemplateRenderTerm(
                        index + 1, term.TermTitle, term.TermTitleEn ?? string.Empty,
                        term.TermContent ?? string.Empty, term.TermContentEn ?? string.Empty)
                    {
                        Kind = (ContractTermKind)term.TermKind,
                        PaymentMilestones = renderMilestones
                    };
                }).ToList()
            },
            hash);
    }

    private static string EscapeCanonical(string? value) =>
        (value ?? string.Empty)
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("|", "\\|", StringComparison.Ordinal)
        .Replace("\r", "\\r", StringComparison.Ordinal)
        .Replace("\n", "\\n", StringComparison.Ordinal);

    private sealed record TemplatePreviewInput(
        ContractTemplateAuthoringPreviewData RenderData,
        string CanonicalHash);

    private static bool IsSha256Hex(string? value) =>
        value is { Length: 64 }
        && value.All(character =>
            character is >= '0' and <= '9'
            or >= 'a' and <= 'f'
            or >= 'A' and <= 'F');

    private async Task CompensateNewArtifactAsync(
        FileStorageResponse? uploadedArtifact)
    {
        if (uploadedArtifact is null || _fileStorageService is null)
        {
            return;
        }

        try
        {
            await _fileStorageService.DeleteUploadedArtifactAsync(uploadedArtifact);
        }
        catch (Exception exception)
        {
            _logger?.LogError(exception,
                "Template DOCX compensation left an orphan artifact {FileId}.",
                uploadedArtifact.FileId);
        }
    }

    private async Task DeleteOldArtifactAfterCommitAsync(int oldFileId)
    {
        try
        {
            await _fileStorageService!.DeleteAsync(oldFileId);
        }
        catch (Exception exception)
        {
            // Version already references the new artifact; retaining the old one
            // is an operational orphan, not a reason to reverse a valid upload.
            _logger?.LogError(exception,
                "Template DOCX replacement left old artifact {FileId} orphaned.",
                oldFileId);
        }
    }

    private static IReadOnlyDictionary<string, object?> BuildAuditValues(
        int? documentFileId,
        string? extension,
        long? sizeBytes,
        string validationStatus,
        int? recognizedPlaceholderCount)
    {
        var values = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["DocumentFileId"] = documentFileId,
            ["ValidationStatus"] = validationStatus
        };
        if (extension is not null)
        {
            values["DocumentExtension"] = GetSafeAuditExtension(extension);
        }

        if (sizeBytes.HasValue)
        {
            values["DocumentSizeBytes"] = Math.Max(sizeBytes.Value, 0);
        }

        if (recognizedPlaceholderCount.HasValue)
        {
            values["RecognizedPlaceholderCount"] = recognizedPlaceholderCount.Value;
        }

        return values;
    }

    private static string GetSafeAuditExtension(string? fileNameOrExtension)
    {
        var extension = Path.GetExtension(fileNameOrExtension ?? string.Empty);
        if (string.IsNullOrEmpty(extension)
            && !string.IsNullOrWhiteSpace(fileNameOrExtension))
        {
            extension = fileNameOrExtension;
        }

        var normalized = extension.Trim().TrimStart('.').ToLowerInvariant();
        return normalized is "doc" or "docx" or "docm" or "dotx" or "dotm"
            ? normalized
            : "other";
    }

    private void EnsureDocumentStorageIsConfigured()
    {
        if (_fileStorageService is null)
        {
            throw new InvalidOperationException(
                "File storage chưa được cấu hình cho Template DOCX.");
        }
    }

    private async Task<ContractTemplateDetailResponse> LoadTemplateDetailAsync(
        TblContractTemplate template,
        CancellationToken cancellationToken)
    {
        var versions = await _dbContext.TblContractTemplateVersions
            .AsNoTracking()
            .Where(version => version.TemplateId == template.TemplateId)
            .OrderByDescending(version => version.VersionNo)
            .ToListAsync(cancellationToken);

        var response = MapTemplateDetail(template);
        response.Versions = versions.Select(MapVersionSummary).ToList();
        return response;
    }

    private async Task<ContractTemplateVersionDetailResponse> LoadVersionDetailAsync(
        int versionId,
        CancellationToken cancellationToken)
    {
        var version = await _dbContext.TblContractTemplateVersions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.TemplateVersionId == versionId,
                cancellationToken);
        if (version is null)
        {
            throw new KeyNotFoundException("Không tìm thấy template version.");
        }

        var template = await GetTemplateAsync(
            version.TemplateId,
            cancellationToken);
        var terms = await _dbContext.TblContractTemplateTerms
            .AsNoTracking()
            .Where(term => term.TemplateVersionId == versionId)
            .OrderBy(term => term.DisplayOrder)
            .ThenBy(term => term.TemplateTermId)
            .ToListAsync(cancellationToken);
        var legalBases = await _dbContext.TblContractTemplateLegalBases
            .AsNoTracking()
            .Where(item => item.TemplateVersionId == versionId)
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.TemplateLegalBasisId)
            .ToListAsync(cancellationToken);
        var paymentMilestones = await _dbContext.TblContractTemplatePaymentMilestones
            .AsNoTracking()
            .Where(item => item.TemplateVersionId == versionId)
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.TemplatePaymentMilestoneId)
            .ToListAsync(cancellationToken);

        var response = MapVersionDetail(version, template);
        response.Terms = terms.Select(MapTerm).ToList();
        foreach (var term in response.Terms)
        {
            term.PaymentMilestones = paymentMilestones
                .Where(item => item.TemplateTermId == term.TemplateTermId)
                .Select(MapPaymentMilestone).ToList();
        }
        response.LegalBases = legalBases.Select(MapLegalBasis).ToList();
        return response;
    }

    private async Task EnsureLegalBasisCodeAndOrderAreAvailableAsync(
        int versionId, string basisCode, int displayOrder, int? excludedId,
        CancellationToken cancellationToken)
    {
        var duplicate = await _dbContext.TblContractTemplateLegalBases.AnyAsync(
            item => item.TemplateVersionId == versionId
                && item.TemplateLegalBasisId != excludedId
                && (item.BasisCode == basisCode || item.DisplayOrder == displayOrder),
            cancellationToken);
        if (duplicate)
        {
            throw new ArgumentException(
                "BasisCode và DisplayOrder phải duy nhất trong template version.");
        }
    }

    private async Task EnsureTermCodeAndDisplayOrderAreAvailableAsync(
        int versionId,
        string termCode,
        int displayOrder,
        int? excludedTermId,
        CancellationToken cancellationToken)
    {
        var duplicateCode = await _dbContext.TblContractTemplateTerms
            .AnyAsync(
                term => term.TemplateVersionId == versionId
                    && term.TermCode == termCode
                    && term.TemplateTermId != excludedTermId,
                cancellationToken);
        if (duplicateCode)
        {
            throw new ArgumentException(
                "TermCode phải duy nhất trong template version.");
        }

        var duplicateDisplayOrder = await _dbContext.TblContractTemplateTerms
            .AnyAsync(
                term => term.TemplateVersionId == versionId
                    && term.DisplayOrder == displayOrder
                    && term.TemplateTermId != excludedTermId,
                cancellationToken);
        if (duplicateDisplayOrder)
        {
            throw new ArgumentException(
                "DisplayOrder phải duy nhất trong template version.");
        }
    }

    private static void EnsureDraft(TblContractTemplateVersion version)
    {
        ContractTemplatePolicy.EnsureCanEdit(
            (TemplateVersionStatus)version.Status);
    }

    private static (string TermCode, string TermTitle, string? TermTitleEn,
        string? TermContent, string? TermContentEn, int DisplayOrder) NormalizeTerm(
        string? termCode,
        string? termTitle,
        string? termTitleEn,
        string? termContent,
        string? termContentEn,
        int displayOrder)
    {
        if (displayOrder < 0)
        {
            throw new ArgumentException("DisplayOrder không được âm.");
        }

        return (
            NormalizeRequired(termCode, 100, nameof(termCode)),
            NormalizeRequired(termTitle, 500, nameof(termTitle)),
            NormalizeOptional(termTitleEn, 500),
            NormalizeOptional(termContent),
            NormalizeOptional(termContentEn),
            displayOrder);
    }

    private async Task EnsureSinglePaymentTermAsync(int versionId,
        ContractTermKind kind, int? excludedTermId, CancellationToken cancellationToken)
    {
        if (kind != ContractTermKind.Payment) return;
        if (await _dbContext.TblContractTemplateTerms.AnyAsync(term =>
                term.TemplateVersionId == versionId
                && term.TemplateTermId != excludedTermId
                && term.TermKind == (byte)ContractTermKind.Payment,
                cancellationToken))
            throw new ArgumentException("Mỗi template version chỉ có một điều khoản thanh toán.");
    }

    private async Task EnsurePaymentTermAsync(int versionId, int termId,
        CancellationToken cancellationToken)
    {
        var valid = await _dbContext.TblContractTemplateTerms.AnyAsync(term =>
            term.TemplateTermId == termId
            && term.TemplateVersionId == versionId
            && term.TermKind == (byte)ContractTermKind.Payment, cancellationToken);
        if (!valid) throw new ArgumentException("Đợt thanh toán chỉ thuộc điều khoản loại Thanh toán.");
    }

    private async Task EnsurePaymentMilestoneUniqueAsync(int versionId, int termId,
        string code, int order, int? excludedId, CancellationToken cancellationToken)
    {
        if (await _dbContext.TblContractTemplatePaymentMilestones.AnyAsync(item =>
                item.TemplateVersionId == versionId && item.TemplateTermId == termId
                && item.TemplatePaymentMilestoneId != excludedId
                && (item.MilestoneCode == code || item.DisplayOrder == order),
                cancellationToken))
            throw new ArgumentException("Mã và thứ tự đợt thanh toán phải duy nhất.");
    }

    private static void ValidateTermKind(ContractTermKind kind)
    {
        if (!Enum.IsDefined(kind)) throw new ArgumentException("Loại điều khoản không hợp lệ.");
    }

    private static (string Code, string TitleVi, string? TitleEn, decimal Percent,
        PaymentDueAnchor Anchor, int Offset, PaymentDayCountMode DayCount,
        string? ConditionVi, string? ConditionEn, int Order)
        NormalizePaymentMilestone(SaveContractTemplatePaymentMilestoneRequest request)
    {
        if (!Enum.IsDefined(request.DueAnchor) || !Enum.IsDefined(request.DayCountMode))
            throw new ArgumentException("Mốc hạn hoặc cách đếm ngày không hợp lệ.");
        if (request.PaymentPercent <= 0 || request.PaymentPercent > 100)
            throw new ArgumentException("Tỷ lệ thanh toán phải lớn hơn 0 và không vượt 100%.");
        if (request.DueOffsetDays < 0 || request.DisplayOrder < 0)
            throw new ArgumentException("Số ngày và thứ tự không được âm.");
        var conditionVi = NormalizeOptional(request.ConditionVi, 2000);
        if (request.DueAnchor == PaymentDueAnchor.Manual && conditionVi is null)
            throw new ArgumentException("Mốc thủ công phải có mô tả điều kiện.");
        return (NormalizeRequired(request.MilestoneCode, 100, nameof(request.MilestoneCode)).ToUpperInvariant(),
            NormalizeRequired(request.TitleVi, 500, nameof(request.TitleVi)),
            NormalizeOptional(request.TitleEn, 500), request.PaymentPercent,
            request.DueAnchor, request.DueOffsetDays, request.DayCountMode,
            conditionVi, NormalizeOptional(request.ConditionEn, 2000), request.DisplayOrder);
    }

    private static (string BasisCode, string ContentVi, string? ContentEn,
        int DisplayOrder) NormalizeLegalBasis(string? basisCode,
        string? contentVi, string? contentEn, int displayOrder)
    {
        if (displayOrder < 0)
        {
            throw new ArgumentException("DisplayOrder không được âm.");
        }

        return (NormalizeRequired(basisCode, 100, nameof(basisCode)),
            NormalizeRequired(contentVi, int.MaxValue, nameof(contentVi)),
            NormalizeOptional(contentEn), displayOrder);
    }

    private static void ValidateLanguageMode(ContractLanguageMode mode)
    {
        if (!Enum.IsDefined(mode))
        {
            throw new ArgumentException("LanguageMode không hợp lệ.");
        }
    }

    private static string NormalizeTemplateCode(string? value)
    {
        var normalized = NormalizeRequired(
                value,
                50,
                nameof(CreateContractTemplateRequest.TemplateCode))
            .ToUpperInvariant();
        if (!TemplateCodePattern.IsMatch(normalized))
        {
            throw new ArgumentException(
                "Mã mẫu chỉ được gồm chữ cái không dấu, chữ số, dấu gạch ngang hoặc gạch dưới; không được bắt đầu hoặc kết thúc bằng dấu gạch.",
                nameof(CreateContractTemplateRequest.TemplateCode));
        }

        return normalized;
    }

    private static string NormalizeRequired(
        string? value,
        int maxLength,
        string parameterName)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException(
                $"{parameterName} không được để trống.",
                parameterName);
        }

        if (normalized.Length > maxLength)
        {
            throw new ArgumentException(
                $"{parameterName} không được vượt quá {maxLength} ký tự.",
                parameterName);
        }

        return normalized;
    }

    private static string? NormalizeOptional(
        string? value,
        int? maxLength = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (maxLength.HasValue && normalized.Length > maxLength.Value)
        {
            throw new ArgumentException(
                $"Giá trị không được vượt quá {maxLength.Value} ký tự.");
        }

        return normalized;
    }

    private static byte[] DecodeRowVersion(
        string? rowVersion,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(rowVersion))
        {
            throw new ArgumentException(
                $"{parameterName} không được để trống.",
                parameterName);
        }

        try
        {
            var bytes = Convert.FromBase64String(rowVersion);
            if (bytes.Length != 8)
            {
                throw new ArgumentException(
                    $"{parameterName} không hợp lệ.",
                    parameterName);
            }

            return bytes;
        }
        catch (FormatException exception)
        {
            throw new ArgumentException(
                $"{parameterName} không đúng định dạng Base64.",
                parameterName,
                exception);
        }
    }

    private static void EnsureRowVersionMatches(
        byte[]? currentRowVersion,
        byte[] expectedRowVersion,
        string resourceName)
    {
        if (currentRowVersion is null
            || !currentRowVersion.AsSpan().SequenceEqual(expectedRowVersion))
        {
            throw new DbUpdateConcurrencyException(
                $"{resourceName} đã được cập nhật bởi request khác.");
        }
    }

    private void SetOriginalRowVersion(
        TblContractTemplate template,
        byte[] expectedRowVersion) =>
        _dbContext.Entry(template)
            .Property(entity => entity.RowVersion)
            .OriginalValue = expectedRowVersion;

    private void SetOriginalRowVersion(
        TblContractTemplateVersion version,
        byte[] expectedRowVersion) =>
        _dbContext.Entry(version)
            .Property(entity => entity.RowVersion)
            .OriginalValue = expectedRowVersion;

    private void SetOriginalRowVersion(
        TblContractTemplateTerm term,
        byte[] expectedRowVersion) =>
        _dbContext.Entry(term)
            .Property(entity => entity.RowVersion)
            .OriginalValue = expectedRowVersion;

    private void SetOriginalRowVersion(
        TblContractTemplateLegalBasis basis,
        byte[] expectedRowVersion) =>
        _dbContext.Entry(basis)
            .Property(entity => entity.RowVersion)
            .OriginalValue = expectedRowVersion;

    private void SetOriginalRowVersion(
        TblContractTemplatePaymentMilestone item,
        byte[] expectedRowVersion) =>
        _dbContext.Entry(item)
            .Property(entity => entity.RowVersion)
            .OriginalValue = expectedRowVersion;

    private void SetSyntheticRowVersionIfNeeded(TblContractTemplate template)
    {
        if (IsInMemoryProvider() && template.RowVersion is not { Length: 8 })
        {
            template.RowVersion = NewSyntheticRowVersion();
        }
    }

    private void SetSyntheticRowVersionIfNeeded(
        TblContractTemplateVersion version)
    {
        if (IsInMemoryProvider() && version.RowVersion is not { Length: 8 })
        {
            version.RowVersion = NewSyntheticRowVersion();
        }
    }

    private void SetSyntheticRowVersionIfNeeded(TblContractTemplateTerm term)
    {
        if (IsInMemoryProvider() && term.RowVersion is not { Length: 8 })
        {
            term.RowVersion = NewSyntheticRowVersion();
        }
    }

    private void SetSyntheticRowVersionIfNeeded(TblContractTemplatePaymentMilestone item)
    {
        if (IsInMemoryProvider() && item.RowVersion is not { Length: 8 })
            item.RowVersion = NewSyntheticRowVersion();
    }

    private void SetSyntheticRowVersionIfNeeded(TblContractTemplateLegalBasis basis)
    {
        if (IsInMemoryProvider() && basis.RowVersion is not { Length: 8 })
        {
            basis.RowVersion = NewSyntheticRowVersion();
        }
    }

    private void RotateTemplateRowVersionIfNeeded(TblContractTemplate template)
    {
        if (IsInMemoryProvider())
        {
            template.RowVersion = NewSyntheticRowVersion();
        }
    }

    private void RotateVersionRowVersionIfNeeded(
        TblContractTemplateVersion version)
    {
        if (IsInMemoryProvider())
        {
            version.RowVersion = NewSyntheticRowVersion();
        }
    }

    private void RotateTermRowVersionIfNeeded(TblContractTemplateTerm term)
    {
        if (IsInMemoryProvider())
        {
            term.RowVersion = NewSyntheticRowVersion();
        }
    }

    private void RotateLegalBasisRowVersionIfNeeded(TblContractTemplateLegalBasis basis)
    {
        if (IsInMemoryProvider())
        {
            basis.RowVersion = NewSyntheticRowVersion();
        }
    }

    private bool IsInMemoryProvider() =>
        _dbContext.Database.ProviderName
            == "Microsoft.EntityFrameworkCore.InMemory";

    private static byte[] NewSyntheticRowVersion() =>
        BitConverter.GetBytes(
            Interlocked.Increment(ref _syntheticRowVersionSeed));

    private static BusinessRuleException
        CreateDraftVersionAlreadyExistsException() => new(
            StatusCodes.Status409Conflict,
            ContractTemplateErrorCodes.DraftVersionAlreadyExists,
            "Mẫu hợp đồng đã có một bản nháp đang làm việc. Hãy tiếp tục chỉnh sửa bản nháp hiện tại.");

    private static bool IsUniqueConstraintViolation(
        DbUpdateException exception)
    {
        for (Exception? current = exception;
             current is not null;
             current = current.InnerException)
        {
            if (current is SqlException { Number: 2601 or 2627 })
            {
                return true;
            }
        }

        return false;
    }

    private async Task<T> ExecuteInTransactionAsync<T>(
        Func<Task<T>> operation,
        CancellationToken cancellationToken)
    {
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database
                .BeginTransactionAsync(cancellationToken);
            try
            {
                var result = await operation();
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch
            {
                try
                {
                    await transaction.RollbackAsync(cancellationToken);
                }
                catch
                {
                    // Preserve the original business/concurrency exception.
                }

                _dbContext.ChangeTracker.Clear();
                throw;
            }
        });
    }

    private async Task ExecuteInTransactionAsync(
        Func<Task> operation,
        CancellationToken cancellationToken)
    {
        await ExecuteInTransactionAsync(
            async () =>
            {
                await operation();
                return true;
            },
            cancellationToken);
    }

    private static ContractTemplateResponse MapTemplate(
        TblContractTemplate template) => new()
        {
            TemplateId = template.TemplateId,
            TemplateCode = template.TemplateCode,
            TemplateName = template.TemplateName,
            TemplateNameEn = template.TemplateNameEn,
            DocumentType = (TemplateDocumentType)template.DocumentType,
            LanguageMode = (ContractLanguageMode)template.LanguageMode,
            Description = template.Description,
            CurrentPublishedVersionId = template.CurrentPublishedVersionId,
            IsActive = template.IsActive,
            CreatedEmployeeId = template.CreatedEmployeeId,
            CreatedDate = template.CreatedDate,
            UpdatedEmployeeId = template.UpdatedEmployeeId,
            UpdatedDate = template.UpdatedDate,
            RowVersion = EncodeRowVersion(template.RowVersion)
        };

    private static ContractTemplateDetailResponse MapTemplateDetail(
        TblContractTemplate template) => new()
        {
            TemplateId = template.TemplateId,
            TemplateCode = template.TemplateCode,
            TemplateName = template.TemplateName,
            TemplateNameEn = template.TemplateNameEn,
            DocumentType = (TemplateDocumentType)template.DocumentType,
            LanguageMode = (ContractLanguageMode)template.LanguageMode,
            Description = template.Description,
            CurrentPublishedVersionId = template.CurrentPublishedVersionId,
            IsActive = template.IsActive,
            CreatedEmployeeId = template.CreatedEmployeeId,
            CreatedDate = template.CreatedDate,
            UpdatedEmployeeId = template.UpdatedEmployeeId,
            UpdatedDate = template.UpdatedDate,
            RowVersion = EncodeRowVersion(template.RowVersion)
        };

    private static ContractTemplateVersionSummaryResponse MapVersionSummary(
        TblContractTemplateVersion version) => new()
        {
            TemplateVersionId = version.TemplateVersionId,
            VersionNo = version.VersionNo,
            ChangeNote = version.ChangeNote,
            Status = (TemplateVersionStatus)version.Status,
            ValidationStatus = (TemplateValidationStatus)version.ValidationStatus,
            DocumentFileId = version.DocumentFileId,
            PublishedPreviewPdfFileId = version.PublishedPreviewPdfFileId,
            RowVersion = EncodeRowVersion(version.RowVersion),
            CreatedDate = version.CreatedDate,
            UpdatedDate = version.UpdatedDate
        };

    private static ContractTemplateVersionDetailResponse MapVersionDetail(
        TblContractTemplateVersion version,
        TblContractTemplate template) => new()
        {
            TemplateVersionId = version.TemplateVersionId,
            TemplateId = template.TemplateId,
            TemplateCode = template.TemplateCode,
            VersionNo = version.VersionNo,
            ChangeNote = version.ChangeNote,
            Status = (TemplateVersionStatus)version.Status,
            ValidationStatus = (TemplateValidationStatus)version.ValidationStatus,
            ValidationMessage = version.ValidationMessage,
            DocumentFileId = version.DocumentFileId,
            DocumentHash = version.DocumentHash,
            PreviewFileId = version.PreviewFileId,
            PublishedPreviewPdfFileId = version.PublishedPreviewPdfFileId,
            PreviewSourceHash = version.PreviewSourceHash,
            PreviewedAt = version.PreviewedAt,
            PreviewedByEmployeeId = version.PreviewedByEmployeeId,
            CreatedDate = version.CreatedDate,
            UpdatedDate = version.UpdatedDate,
            RowVersion = EncodeRowVersion(version.RowVersion)
        };

    private static ContractTemplatePreviewResponse MapPreviewResponse(
        TblContractTemplateVersion version,
        bool isReused)
    {
        if (version.PreviewFileId is not > 0
            || version.PreviewedAt is null
            || version.PreviewedByEmployeeId is not > 0)
        {
            throw new InvalidOperationException(
                "Preview hiện hành thiếu metadata bắt buộc.");
        }

        return new ContractTemplatePreviewResponse
        {
            TemplateVersionId = version.TemplateVersionId,
            PreviewFileId = version.PreviewFileId.Value,
            PreviewedAt = version.PreviewedAt.Value,
            PreviewedByEmployeeId = version.PreviewedByEmployeeId.Value,
            IsCurrent = true,
            IsReused = isReused,
            RowVersion = EncodeRowVersion(version.RowVersion)
        };
    }

    private static ContractTemplateTermResponse MapTerm(
        TblContractTemplateTerm term) => new()
        {
            TemplateTermId = term.TemplateTermId,
            TemplateVersionId = term.TemplateVersionId,
            TermCode = term.TermCode,
            TermTitle = term.TermTitle,
            TermTitleEn = term.TermTitleEn,
            TermContent = term.TermContent,
            TermContentEn = term.TermContentEn,
            IsNegotiable = term.IsNegotiable,
            DisplayOrder = term.DisplayOrder,
            TermKind = (ContractTermKind)term.TermKind,
            CreatedEmployeeId = term.CreatedEmployeeId,
            CreatedDate = term.CreatedDate,
            UpdatedEmployeeId = term.UpdatedEmployeeId,
            UpdatedDate = term.UpdatedDate,
            RowVersion = EncodeRowVersion(term.RowVersion)
        };

    private static ContractTemplatePaymentMilestoneResponse MapPaymentMilestone(
        TblContractTemplatePaymentMilestone item) => new()
        {
            TemplatePaymentMilestoneId = item.TemplatePaymentMilestoneId,
            TemplateVersionId = item.TemplateVersionId,
            TemplateTermId = item.TemplateTermId,
            MilestoneCode = item.MilestoneCode,
            TitleVi = item.TitleVi,
            TitleEn = item.TitleEn,
            PaymentPercent = item.PaymentPercent,
            DueAnchor = (PaymentDueAnchor)item.DueAnchor,
            DueOffsetDays = item.DueOffsetDays,
            DayCountMode = (PaymentDayCountMode)item.DayCountMode,
            ConditionVi = item.ConditionVi,
            ConditionEn = item.ConditionEn,
            DisplayOrder = item.DisplayOrder,
            RowVersion = EncodeRowVersion(item.RowVersion)
        };

    private static ContractTemplateLegalBasisResponse MapLegalBasis(
        TblContractTemplateLegalBasis basis) => new()
        {
            TemplateLegalBasisId = basis.TemplateLegalBasisId,
            TemplateVersionId = basis.TemplateVersionId,
            BasisCode = basis.BasisCode,
            ContentVi = basis.ContentVi,
            ContentEn = basis.ContentEn,
            DisplayOrder = basis.DisplayOrder,
            CreatedEmployeeId = basis.CreatedEmployeeId,
            CreatedDate = basis.CreatedDate,
            UpdatedEmployeeId = basis.UpdatedEmployeeId,
            UpdatedDate = basis.UpdatedDate,
            RowVersion = EncodeRowVersion(basis.RowVersion)
        };

    private static void TouchVersion(
        TblContractTemplateVersion version,
        int employeeId,
        DateTime now)
    {
        version.UpdatedEmployeeId = employeeId;
        version.UpdatedDate = now;
    }

    private static string EncodeRowVersion(byte[]? rowVersion) =>
        rowVersion is { Length: > 0 }
            ? Convert.ToBase64String(rowVersion)
            : string.Empty;

    private static bool IsTemplateCodeUniqueViolation(
        DbUpdateException exception)
    {
        var message = exception.ToString();
        return message.Contains(
                   "UX_tbl_ContractTemplate_TemplateCode",
                   StringComparison.OrdinalIgnoreCase)
            || message.Contains("duplicate", StringComparison.OrdinalIgnoreCase)
            || message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase);
    }
}
