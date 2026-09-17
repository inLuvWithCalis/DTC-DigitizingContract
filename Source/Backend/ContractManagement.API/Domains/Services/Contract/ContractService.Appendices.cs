using ContractManagement.API.Common.Enums;
using ContractManagement.API.Domains.DTOs.Requests.Contract;
using ContractManagement.API.Domains.DTOs.Responses.Contract;
using ContractManagement.Domains.Interfaces.Contract;
using ContractManagement.Infrastructure.Persistence.Application.Models;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Domains.Services.Contract;

public partial class ContractService
{
    public async Task<ContractAppendixResponse> AddAppendixAsync(
        int contractId, int versionId, AddContractAppendixRequest request,
        int employeeId)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (contract, version) = await LoadWritableAppendixStateAsync(
            contractId, versionId, employeeId, request.VersionRowVersion);
        var source = await _dbContext.TblContractTemplateAppendices
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.TemplateAppendixId ==
                    request.TemplateAppendixId
                && x.TemplateVersionId == version.TemplateVersionId)
            ?? throw new ArgumentException(
                "Phụ lục không thuộc template version của hợp đồng.");
        if (source.IsRequired)
            throw new InvalidOperationException(
                "Phụ lục bắt buộc phải được tạo cùng hợp đồng.");
        if (await _dbContext.TblContractAppendices.AnyAsync(x =>
                x.VersionId == versionId
                && x.SourceTemplateAppendixId == source.TemplateAppendixId))
            throw new InvalidOperationException("Phụ lục đã tồn tại trong version.");

        var sourceTerms = await _dbContext.TblContractTemplateAppendixTerms
            .AsNoTracking()
            .Where(x => x.TemplateAppendixId == source.TemplateAppendixId)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.TemplateAppendixTermId)
            .ToListAsync();
        if (sourceTerms.Count == 0)
            throw new InvalidOperationException("Phụ lục template chưa có điều khoản.");

        var now = DateTime.UtcNow;
        var displayOrder = (await _dbContext.TblContractAppendices
            .Where(x => x.VersionId == versionId)
            .Select(x => (int?)x.DisplayOrder)
            .MaxAsync() ?? 0) + 1;
        var row = new TblContractAppendix
        {
            ContractId = contractId,
            VersionId = versionId,
            SourceTemplateAppendixId = source.TemplateAppendixId,
            AppendixCode = source.AppendixCode,
            AppendixName = source.AppendixName,
            AppendixNameEn = source.AppendixNameEn,
            AppendixDescription = source.AppendixDescription,
            IsRequired = false,
            DisplayOrder = displayOrder,
            CreatedEmployeeId = employeeId,
            CreatedDate = now
        };
        _dbContext.TblContractAppendices.Add(row);
        await _dbContext.SaveChangesAsync();
        _dbContext.TblContractAppendixTerms.AddRange(sourceTerms.Select(term =>
            new TblContractAppendixTerm
            {
                AppendixId = row.AppendixId,
                SourceTemplateAppendixTermId = term.TemplateAppendixTermId,
                TermCode = term.TermCode,
                TermTitle = term.TermTitle,
                TermTitleEn = term.TermTitleEn,
                TermContent = term.TermContent,
                TermContentEn = term.TermContentEn,
                DisplayOrder = term.DisplayOrder,
                CreatedEmployeeId = employeeId,
                CreatedDate = now
            }));
        await TouchAppendixAggregateAsync(contract, version, employeeId, now);
        await StageAppendixAuditAsync(contractId, versionId, row.AppendixId,
            employeeId, now, ContractAuditActionTypes.ContractAppendixSelected);
        await _dbContext.SaveChangesAsync();
        return await LoadContractAppendixAsync(row.AppendixId);
    }

    public async Task DeleteAppendixAsync(
        int contractId, int versionId, int appendixId,
        DeleteContractAppendixRequest request, int employeeId)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (contract, version) = await LoadWritableAppendixStateAsync(
            contractId, versionId, employeeId, request.VersionRowVersion);
        var row = await RequireAppendixAsync(contractId, versionId, appendixId);
        EnsureRowVersionMatches(row.RowVersion,
            DecodeRowVersion(request.RowVersion, nameof(request.RowVersion)),
            "Phụ lục hợp đồng");
        if (row.IsRequired)
            throw new InvalidOperationException("Không thể xóa phụ lục bắt buộc.");
        var terms = await _dbContext.TblContractAppendixTerms
            .Where(x => x.AppendixId == appendixId).ToListAsync();
        _dbContext.TblContractAppendixTerms.RemoveRange(terms);
        _dbContext.TblContractAppendices.Remove(row);
        var now = DateTime.UtcNow;
        await TouchAppendixAggregateAsync(contract, version, employeeId, now);
        await StageAppendixAuditAsync(contractId, versionId, appendixId,
            employeeId, now, ContractAuditActionTypes.ContractAppendixRemoved);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<ContractAppendixTermResponse> AddAppendixTermAsync(
        int contractId, int versionId, int appendixId,
        CreateContractAppendixTermRequest request, int employeeId)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (contract, version) = await LoadWritableAppendixStateAsync(
            contractId, versionId, employeeId, request.VersionRowVersion);
        var appendix = await RequireAppendixAsync(contractId, versionId, appendixId);
        MatchRowVersion(appendix.RowVersion, request.AppendixRowVersion,
            "Phụ lục hợp đồng");
        var code = NormalizeAppendixTermCode(request.TermCode);
        await EnsureAppendixTermCodeUniqueAsync(appendixId, code, null);
        var row = new TblContractAppendixTerm
        {
            AppendixId = appendixId,
            SourceTemplateAppendixTermId = null,
            TermCode = code,
            TermTitle = NormalizeAppendixRequired(request.TermTitle, "Tiêu đề"),
            TermTitleEn = NormalizeOptional(request.TermTitleEn),
            TermContent = NormalizeRichText(request.TermContent),
            TermContentEn = NormalizeRichText(request.TermContentEn),
            DisplayOrder = request.DisplayOrder > 0
                ? request.DisplayOrder
                : (await _dbContext.TblContractAppendixTerms
                    .Where(x => x.AppendixId == appendixId)
                    .Select(x => (int?)x.DisplayOrder).MaxAsync() ?? 0) + 1,
            CreatedEmployeeId = employeeId,
            CreatedDate = DateTime.UtcNow
        };
        ValidateAppendixTermLanguage(contract, row);
        _dbContext.TblContractAppendixTerms.Add(row);
        await TouchAppendixParentAsync(appendix, contract, version, employeeId,
            ContractAuditActionTypes.ContractAppendixTermCreated);
        await _dbContext.SaveChangesAsync();
        return MapAppendixTerm(row);
    }

    public async Task<ContractAppendixTermResponse> UpdateAppendixTermAsync(
        int contractId, int versionId, int appendixId, int termId,
        UpdateContractAppendixTermRequest request, int employeeId)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (contract, version) = await LoadWritableAppendixStateAsync(
            contractId, versionId, employeeId, request.VersionRowVersion);
        var appendix = await RequireAppendixAsync(contractId, versionId, appendixId);
        MatchRowVersion(appendix.RowVersion, request.AppendixRowVersion,
            "Phụ lục hợp đồng");
        var row = await RequireAppendixTermAsync(appendixId, termId);
        MatchRowVersion(row.RowVersion, request.RowVersion, "Điều khoản phụ lục");
        var code = NormalizeAppendixTermCode(request.TermCode);
        await EnsureAppendixTermCodeUniqueAsync(appendixId, code, termId);
        row.TermCode = code;
        row.TermTitle = NormalizeAppendixRequired(request.TermTitle, "Tiêu đề");
        row.TermTitleEn = NormalizeOptional(request.TermTitleEn);
        row.TermContent = NormalizeRichText(request.TermContent);
        row.TermContentEn = NormalizeRichText(request.TermContentEn);
        row.DisplayOrder = request.DisplayOrder;
        row.UpdatedEmployeeId = employeeId;
        row.UpdatedDate = DateTime.UtcNow;
        ValidateAppendixTermLanguage(contract, row);
        await TouchAppendixParentAsync(appendix, contract, version, employeeId,
            ContractAuditActionTypes.ContractAppendixTermUpdated);
        await _dbContext.SaveChangesAsync();
        return MapAppendixTerm(row);
    }

    public async Task DeleteAppendixTermAsync(
        int contractId, int versionId, int appendixId, int termId,
        DeleteContractAppendixTermRequest request, int employeeId)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (contract, version) = await LoadWritableAppendixStateAsync(
            contractId, versionId, employeeId, request.VersionRowVersion);
        var appendix = await RequireAppendixAsync(contractId, versionId, appendixId);
        MatchRowVersion(appendix.RowVersion, request.AppendixRowVersion,
            "Phụ lục hợp đồng");
        var row = await RequireAppendixTermAsync(appendixId, termId);
        MatchRowVersion(row.RowVersion, request.RowVersion, "Điều khoản phụ lục");
        _dbContext.TblContractAppendixTerms.Remove(row);
        await TouchAppendixParentAsync(appendix, contract, version, employeeId,
            ContractAuditActionTypes.ContractAppendixTermDeleted);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<ContractAppendixResponse> ReorderAppendixTermsAsync(
        int contractId, int versionId, int appendixId,
        ReorderContractAppendixTermsRequest request, int employeeId)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (contract, version) = await LoadWritableAppendixStateAsync(
            contractId, versionId, employeeId, request.VersionRowVersion);
        var appendix = await RequireAppendixAsync(contractId, versionId, appendixId);
        MatchRowVersion(appendix.RowVersion, request.AppendixRowVersion,
            "Phụ lục hợp đồng");
        var rows = await _dbContext.TblContractAppendixTerms
            .Where(x => x.AppendixId == appendixId).ToListAsync();
        if (request.Terms.Count != rows.Count
            || request.Terms.Select(x => x.AppendixTermId).Distinct().Count()
                != request.Terms.Count
            || request.Terms.Any(item => rows.All(row =>
                row.AppendixTermId != item.AppendixTermId))
            || request.Terms.Select(x => x.DisplayOrder).Distinct().Count()
                != request.Terms.Count)
            throw new ArgumentException(
                "Danh sách sắp xếp phải chứa đúng mỗi điều khoản một lần.");
        var now = DateTime.UtcNow;
        foreach (var item in request.Terms)
        {
            var row = rows.Single(x => x.AppendixTermId == item.AppendixTermId);
            MatchRowVersion(row.RowVersion, item.RowVersion,
                "Điều khoản phụ lục");
            row.DisplayOrder = item.DisplayOrder;
            row.UpdatedEmployeeId = employeeId;
            row.UpdatedDate = now;
        }
        await TouchAppendixParentAsync(appendix, contract, version, employeeId,
            ContractAuditActionTypes.ContractAppendixTermsReordered);
        await _dbContext.SaveChangesAsync();
        return await LoadContractAppendixAsync(appendixId);
    }

    private async Task<(TblContract Contract, TblContractVersion Version)>
        LoadWritableAppendixStateAsync(int contractId, int versionId,
            int employeeId, string versionRowVersion)
    {
        if (contractId <= 0 || versionId <= 0 || employeeId <= 0)
            throw new ArgumentException("Contract, version và employee phải hợp lệ.");
        var contract = await _dbContext.TblContracts.SingleOrDefaultAsync(x =>
            x.ContractId == contractId && x.EmployeeId == employeeId)
            ?? throw new KeyNotFoundException("Không tìm thấy hợp đồng.");
        if (contract.CurrentVersionId != versionId
            || (ContractStatus)contract.Status is not (
                ContractStatus.Draft or ContractStatus.Negotiating))
            throw new InvalidOperationException(
                "Chỉ version hiện hành đang chỉnh sửa mới được thay đổi phụ lục.");
        var version = await _dbContext.TblContractVersions.SingleOrDefaultAsync(x =>
            x.ContractId == contractId && x.VersionId == versionId)
            ?? throw new KeyNotFoundException("Không tìm thấy version hợp đồng.");
        if (version.IsLocked)
            throw new InvalidOperationException("Version đã khóa không thể sửa phụ lục.");
        var expected = DecodeRowVersion(versionRowVersion,
            nameof(versionRowVersion));
        EnsureRowVersionMatches(version.RowVersion, expected, "Version hợp đồng");
        _dbContext.Entry(version).Property(x => x.RowVersion).OriginalValue = expected;
        return (contract, version);
    }

    private async Task<TblContractAppendix> RequireAppendixAsync(
        int contractId, int versionId, int appendixId) =>
        await _dbContext.TblContractAppendices.SingleOrDefaultAsync(x =>
            x.AppendixId == appendixId && x.ContractId == contractId
            && x.VersionId == versionId)
        ?? throw new KeyNotFoundException("Không tìm thấy phụ lục hợp đồng.");

    private async Task<TblContractAppendixTerm> RequireAppendixTermAsync(
        int appendixId, int termId) =>
        await _dbContext.TblContractAppendixTerms.SingleOrDefaultAsync(x =>
            x.AppendixTermId == termId && x.AppendixId == appendixId)
        ?? throw new KeyNotFoundException("Không tìm thấy điều khoản phụ lục.");

    private async Task EnsureAppendixTermCodeUniqueAsync(
        int appendixId, string code, int? excludedId)
    {
        if (await _dbContext.TblContractAppendixTerms.AnyAsync(x =>
                x.AppendixId == appendixId && x.TermCode == code
                && (!excludedId.HasValue || x.AppendixTermId != excludedId.Value)))
            throw new InvalidOperationException("Mã điều khoản phụ lục đã tồn tại.");
    }

    private static string NormalizeAppendixTermCode(string value) =>
        NormalizeAppendixRequired(value, "Mã điều khoản").ToUpperInvariant();

    private static string NormalizeAppendixRequired(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{field} không được để trống.");
        return value.Trim();
    }

    private static void ValidateAppendixTermLanguage(
        TblContract contract, TblContractAppendixTerm term)
    {
        if ((ContractLanguageMode)contract.LanguageMode ==
                ContractLanguageMode.Bilingual
            && (string.IsNullOrWhiteSpace(term.TermTitleEn)
                || (!string.IsNullOrWhiteSpace(term.TermContent)
                    && string.IsNullOrWhiteSpace(term.TermContentEn))))
            throw new ArgumentException(
                "Điều khoản phụ lục song ngữ phải có đủ tiêu đề và nội dung tiếng Anh.");
    }

    private static void MatchRowVersion(
        byte[] actual, string encodedExpected, string resource) =>
        EnsureRowVersionMatches(actual,
            DecodeRowVersion(encodedExpected, nameof(encodedExpected)), resource);

    private async Task TouchAppendixParentAsync(TblContractAppendix appendix,
        TblContract contract, TblContractVersion version, int employeeId,
        string auditAction)
    {
        appendix.UpdatedEmployeeId = employeeId;
        appendix.UpdatedDate = DateTime.UtcNow;
        await TouchAppendixAggregateAsync(contract, version, employeeId,
            appendix.UpdatedDate.Value);
        await StageAppendixAuditAsync(contract.ContractId, version.VersionId,
            appendix.AppendixId, employeeId, appendix.UpdatedDate.Value,
            auditAction);
    }

    private Task TouchAppendixAggregateAsync(TblContract contract,
        TblContractVersion version, int employeeId, DateTime now)
    {
        contract.UpdatedEmployeeId = employeeId;
        contract.UpdateDate = now;
        version.SnapshotHash = null;
        _dbContext.Entry(version).Property(x => x.SnapshotHash).IsModified = true;
        return Task.CompletedTask;
    }

    private async Task StageAppendixAuditAsync(int contractId, int versionId,
        int appendixId, int employeeId, DateTime now, string action)
    {
        var count = await _dbContext.TblContractAppendices
            .CountAsync(x => x.VersionId == versionId);
        count += _dbContext.ChangeTracker.Entries<TblContractAppendix>()
            .Count(entry => entry.Entity.VersionId == versionId
                && entry.State == EntityState.Added);
        count -= _dbContext.ChangeTracker.Entries<TblContractAppendix>()
            .Count(entry => entry.Entity.VersionId == versionId
                && entry.State == EntityState.Deleted);
        _contractAuditWriter.StageEmployeeAudits(
        [
            new EmployeeContractAuditWriteRequest(
                contractId, versionId, employeeId,
                action,
                ContractAuditResults.Succeeded, now,
                SubjectType: ContractAuditSubjectTypes.ContractAppendix,
                SubjectId: appendixId,
                NewValues: ContractAuditValues.Create(
                    ("CurrentVersionId", versionId),
                    ("AppendixCount", count)))
        ]);
    }

    private async Task<ContractAppendixResponse> LoadContractAppendixAsync(
        int appendixId)
    {
        var row = await _dbContext.TblContractAppendices.AsNoTracking()
            .SingleAsync(x => x.AppendixId == appendixId);
        var terms = await _dbContext.TblContractAppendixTerms.AsNoTracking()
            .Where(x => x.AppendixId == appendixId)
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.AppendixTermId)
            .ToListAsync();
        return MapAppendix(row, terms);
    }

    private async Task<List<ContractAppendixResponse>>
        LoadContractAppendicesAsync(int contractId, int versionId)
    {
        var rows = await _dbContext.TblContractAppendices.AsNoTracking()
            .Where(x => x.ContractId == contractId && x.VersionId == versionId)
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.AppendixId)
            .ToListAsync();
        var ids = rows.Select(x => x.AppendixId).ToList();
        var terms = await _dbContext.TblContractAppendixTerms.AsNoTracking()
            .Where(x => ids.Contains(x.AppendixId))
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.AppendixTermId)
            .ToListAsync();
        return rows.Select(row => MapAppendix(row,
            terms.Where(x => x.AppendixId == row.AppendixId))).ToList();
    }

    private async Task<List<ContractAppendixOptionResponse>>
        LoadAvailableOptionalAppendicesAsync(
            int templateVersionId,
            IEnumerable<ContractAppendixResponse> currentAppendices)
    {
        var selectedSourceIds = currentAppendices
            .Select(item => item.SourceTemplateAppendixId)
            .ToList();
        return await _dbContext.TblContractTemplateAppendices
            .AsNoTracking()
            .Where(item => item.TemplateVersionId == templateVersionId
                && !item.IsRequired
                && !selectedSourceIds.Contains(item.TemplateAppendixId))
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.TemplateAppendixId)
            .Select(item => new ContractAppendixOptionResponse
            {
                TemplateAppendixId = item.TemplateAppendixId,
                AppendixCode = item.AppendixCode,
                AppendixName = item.AppendixName,
                AppendixNameEn = item.AppendixNameEn,
                AppendixDescription = item.AppendixDescription,
                DisplayOrder = item.DisplayOrder
            })
            .ToListAsync();
    }

    private static ContractAppendixResponse MapAppendix(
        TblContractAppendix row, IEnumerable<TblContractAppendixTerm> terms) =>
        new()
        {
            AppendixId = row.AppendixId,
            ContractId = row.ContractId,
            VersionId = row.VersionId,
            SourceTemplateAppendixId = row.SourceTemplateAppendixId,
            AppendixCode = row.AppendixCode,
            AppendixName = row.AppendixName,
            AppendixNameEn = row.AppendixNameEn,
            AppendixDescription = row.AppendixDescription,
            IsRequired = row.IsRequired,
            DisplayOrder = row.DisplayOrder,
            RowVersion = EncodeRowVersion(row.RowVersion),
            Terms = terms.Select(MapAppendixTerm).ToList()
        };

    private static ContractAppendixTermResponse MapAppendixTerm(
        TblContractAppendixTerm row) => new()
        {
            AppendixTermId = row.AppendixTermId,
            AppendixId = row.AppendixId,
            SourceTemplateAppendixTermId = row.SourceTemplateAppendixTermId,
            TermCode = row.TermCode,
            TermTitle = row.TermTitle,
            TermTitleEn = row.TermTitleEn,
            TermContent = row.TermContent,
            TermContentEn = row.TermContentEn,
            DisplayOrder = row.DisplayOrder,
            RowVersion = EncodeRowVersion(row.RowVersion)
        };
}
