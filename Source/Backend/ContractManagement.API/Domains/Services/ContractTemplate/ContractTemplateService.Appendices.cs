using ContractManagement.API.Common.Enums;
using ContractManagement.API.Domains.DTOs.Requests.ContractTemplate;
using ContractManagement.API.Domains.DTOs.Responses.ContractTemplate;
using ContractManagement.Common.Enums;
using ContractManagement.Domains.Interfaces.ContractTemplate;
using ContractManagement.Infrastructure.Persistence.Application.Models;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Domains.Services.ContractTemplate;

public sealed partial class ContractTemplateService
{
    public async Task<ContractTemplateAppendixResponse> AddAppendixAsync(
        int versionId, CreateContractTemplateAppendixRequest request,
        int employeeId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await EnsureAdminOfficerAsync(employeeId, cancellationToken);
        var values = NormalizeAppendix(request);
        var versionRv = DecodeRowVersion(request.VersionRowVersion,
            nameof(request.VersionRowVersion));
        return await ExecuteInTransactionAsync(async () =>
        {
            var version = await GetVersionForMutationAsync(versionId, cancellationToken);
            EnsureDraft(version);
            MatchAndTrack(version, versionRv);
            await EnsureAppendixUniqueAsync(versionId, values.Code, values.Order,
                null, cancellationToken);
            var now = DateTime.UtcNow;
            var row = new TblContractTemplateAppendix
            {
                TemplateVersionId = versionId, AppendixCode = values.Code,
                AppendixName = values.Name, AppendixNameEn = values.NameEn,
                AppendixDescription = values.Description,
                IsRequired = values.Required,
                IsSelectedByDefault = values.SelectedByDefault,
                DisplayOrder = values.Order, CreatedEmployeeId = employeeId,
                CreatedDate = now
            };
            SetSyntheticRowVersionIfNeeded(row);
            _dbContext.TblContractTemplateAppendices.Add(row);
            InvalidateAppendixPreview(version, employeeId, now);
            await _dbContext.SaveChangesAsync(cancellationToken);
            StageAppendixAudit(version, employeeId,
                ContractTemplateAuditActionTypes.TemplateAppendixCreated,
                row.TemplateAppendixId, null);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return await LoadAppendixAsync(versionId, row.TemplateAppendixId,
                cancellationToken);
        }, cancellationToken);
    }

    public async Task<ContractTemplateAppendixResponse> UpdateAppendixAsync(
        int versionId, int appendixId,
        UpdateContractTemplateAppendixRequest request, int employeeId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await EnsureAdminOfficerAsync(employeeId, cancellationToken);
        var values = NormalizeAppendix(request);
        var versionRv = DecodeRowVersion(request.VersionRowVersion,
            nameof(request.VersionRowVersion));
        var appendixRv = DecodeRowVersion(request.RowVersion,
            nameof(request.RowVersion));
        return await ExecuteInTransactionAsync(async () =>
        {
            var version = await GetVersionForMutationAsync(versionId, cancellationToken);
            EnsureDraft(version); MatchAndTrack(version, versionRv);
            var row = await GetAppendixForMutationAsync(versionId, appendixId,
                cancellationToken);
            MatchAndTrack(row, appendixRv);
            await EnsureAppendixUniqueAsync(versionId, values.Code, values.Order,
                appendixId, cancellationToken);
            row.AppendixCode = values.Code; row.AppendixName = values.Name;
            row.AppendixNameEn = values.NameEn;
            row.AppendixDescription = values.Description;
            row.IsRequired = values.Required;
            row.IsSelectedByDefault = values.SelectedByDefault;
            row.DisplayOrder = values.Order;
            row.UpdatedEmployeeId = employeeId; row.UpdatedDate = DateTime.UtcNow;
            RotateRowVersionIfNeeded(row);
            InvalidateAppendixPreview(version, employeeId, row.UpdatedDate.Value);
            StageAppendixAudit(version, employeeId,
                ContractTemplateAuditActionTypes.TemplateAppendixUpdated,
                appendixId, null);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return await LoadAppendixAsync(versionId, appendixId, cancellationToken);
        }, cancellationToken);
    }

    public async Task DeleteAppendixAsync(int versionId, int appendixId,
        DeleteContractTemplateAppendixRequest request, int employeeId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await EnsureAdminOfficerAsync(employeeId, cancellationToken);
        var versionRv = DecodeRowVersion(request.VersionRowVersion,
            nameof(request.VersionRowVersion));
        var appendixRv = DecodeRowVersion(request.RowVersion, nameof(request.RowVersion));
        await ExecuteInTransactionAsync(async () =>
        {
            var version = await GetVersionForMutationAsync(versionId, cancellationToken);
            EnsureDraft(version); MatchAndTrack(version, versionRv);
            var row = await GetAppendixForMutationAsync(versionId, appendixId,
                cancellationToken);
            MatchAndTrack(row, appendixRv);
            var terms = await _dbContext.TblContractTemplateAppendixTerms
                .Where(x => x.TemplateAppendixId == appendixId).ToListAsync(cancellationToken);
            _dbContext.TblContractTemplateAppendixTerms.RemoveRange(terms);
            _dbContext.TblContractTemplateAppendices.Remove(row);
            InvalidateAppendixPreview(version, employeeId, DateTime.UtcNow);
            StageAppendixAudit(version, employeeId,
                ContractTemplateAuditActionTypes.TemplateAppendixDeleted,
                appendixId, terms.Count);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }, cancellationToken);
    }

    public async Task<ContractTemplateVersionDetailResponse> ReorderAppendicesAsync(
        int versionId, ReorderContractTemplateAppendicesRequest request,
        int employeeId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await EnsureAdminOfficerAsync(employeeId, cancellationToken);
        ValidateReorder(request.Appendices, x => x.TemplateAppendixId,
            x => x.DisplayOrder, "Appendix");
        var versionRv = DecodeRowVersion(request.VersionRowVersion,
            nameof(request.VersionRowVersion));
        return await ExecuteInTransactionAsync(async () =>
        {
            var version = await GetVersionForMutationAsync(versionId, cancellationToken);
            EnsureDraft(version); MatchAndTrack(version, versionRv);
            var rows = await _dbContext.TblContractTemplateAppendices
                .Where(x => x.TemplateVersionId == versionId).ToListAsync(cancellationToken);
            var byId = request.Appendices.ToDictionary(x => x.TemplateAppendixId);
            if (!rows.Select(x => x.TemplateAppendixId).ToHashSet().SetEquals(byId.Keys))
                throw new ArgumentException("Danh sách reorder phải chứa đúng toàn bộ Appendix.");
            foreach (var row in rows)
            {
                var item = byId[row.TemplateAppendixId];
                MatchAndTrack(row, DecodeRowVersion(item.RowVersion, nameof(item.RowVersion)));
                row.DisplayOrder = item.DisplayOrder; row.UpdatedEmployeeId = employeeId;
                row.UpdatedDate = DateTime.UtcNow; RotateRowVersionIfNeeded(row);
            }
            InvalidateAppendixPreview(version, employeeId, DateTime.UtcNow);
            StageAppendixAudit(version, employeeId,
                ContractTemplateAuditActionTypes.TemplateAppendicesReordered,
                null, rows.Count);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return await LoadVersionDetailAsync(versionId, cancellationToken);
        }, cancellationToken);
    }

    public async Task<ContractTemplateAppendixTermResponse> AddAppendixTermAsync(
        int versionId, int appendixId,
        CreateContractTemplateAppendixTermRequest request, int employeeId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await EnsureAdminOfficerAsync(employeeId, cancellationToken);
        var values = NormalizeAppendixTerm(request.TermCode, request.TermTitle,
            request.TermTitleEn, request.TermContent, request.TermContentEn,
            request.DisplayOrder);
        var versionRv = DecodeRowVersion(request.VersionRowVersion,
            nameof(request.VersionRowVersion));
        var appendixRv = DecodeRowVersion(request.AppendixRowVersion,
            nameof(request.AppendixRowVersion));
        return await ExecuteInTransactionAsync(async () =>
        {
            var version = await GetVersionForMutationAsync(versionId, cancellationToken);
            EnsureDraft(version); MatchAndTrack(version, versionRv);
            var appendix = await GetAppendixForMutationAsync(versionId, appendixId,
                cancellationToken); MatchAndTrack(appendix, appendixRv);
            await EnsureAppendixTermUniqueAsync(appendixId, values.Code,
                values.Order, null, cancellationToken);
            var now = DateTime.UtcNow;
            var row = new TblContractTemplateAppendixTerm
            {
                TemplateAppendixId = appendixId, TermCode = values.Code,
                TermTitle = values.Title, TermTitleEn = values.TitleEn,
                TermContent = values.Content, TermContentEn = values.ContentEn,
                DisplayOrder = values.Order, CreatedEmployeeId = employeeId,
                CreatedDate = now
            };
            SetSyntheticRowVersionIfNeeded(row);
            _dbContext.TblContractTemplateAppendixTerms.Add(row);
            appendix.UpdatedEmployeeId = employeeId; appendix.UpdatedDate = now;
            RotateRowVersionIfNeeded(appendix);
            InvalidateAppendixPreview(version, employeeId, now);
            await _dbContext.SaveChangesAsync(cancellationToken);
            StageAppendixAudit(version, employeeId,
                ContractTemplateAuditActionTypes.TemplateAppendixTermCreated,
                appendixId, null, row.TemplateAppendixTermId);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return MapAppendixTerm(row);
        }, cancellationToken);
    }

    public async Task<ContractTemplateAppendixTermResponse> UpdateAppendixTermAsync(
        int versionId, int appendixId, int termId,
        UpdateContractTemplateAppendixTermRequest request, int employeeId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await EnsureAdminOfficerAsync(employeeId, cancellationToken);
        var values = NormalizeAppendixTerm(request.TermCode, request.TermTitle,
            request.TermTitleEn, request.TermContent, request.TermContentEn,
            request.DisplayOrder);
        var versionRv = DecodeRowVersion(request.VersionRowVersion, nameof(request.VersionRowVersion));
        var appendixRv = DecodeRowVersion(request.AppendixRowVersion, nameof(request.AppendixRowVersion));
        var termRv = DecodeRowVersion(request.RowVersion, nameof(request.RowVersion));
        return await ExecuteInTransactionAsync(async () =>
        {
            var version = await GetVersionForMutationAsync(versionId, cancellationToken);
            EnsureDraft(version); MatchAndTrack(version, versionRv);
            var appendix = await GetAppendixForMutationAsync(versionId, appendixId, cancellationToken);
            MatchAndTrack(appendix, appendixRv);
            var row = await GetAppendixTermForMutationAsync(appendixId, termId, cancellationToken);
            MatchAndTrack(row, termRv);
            await EnsureAppendixTermUniqueAsync(appendixId, values.Code,
                values.Order, termId, cancellationToken);
            row.TermCode = values.Code; row.TermTitle = values.Title;
            row.TermTitleEn = values.TitleEn; row.TermContent = values.Content;
            row.TermContentEn = values.ContentEn; row.DisplayOrder = values.Order;
            row.UpdatedEmployeeId = employeeId; row.UpdatedDate = DateTime.UtcNow;
            RotateRowVersionIfNeeded(row);
            appendix.UpdatedEmployeeId = employeeId; appendix.UpdatedDate = row.UpdatedDate;
            RotateRowVersionIfNeeded(appendix);
            InvalidateAppendixPreview(version, employeeId, row.UpdatedDate.Value);
            StageAppendixAudit(version, employeeId,
                ContractTemplateAuditActionTypes.TemplateAppendixTermUpdated,
                appendixId, null, termId);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return MapAppendixTerm(row);
        }, cancellationToken);
    }

    public async Task DeleteAppendixTermAsync(int versionId, int appendixId,
        int termId, DeleteContractTemplateAppendixTermRequest request,
        int employeeId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await EnsureAdminOfficerAsync(employeeId, cancellationToken);
        var versionRv = DecodeRowVersion(request.VersionRowVersion, nameof(request.VersionRowVersion));
        var appendixRv = DecodeRowVersion(request.AppendixRowVersion, nameof(request.AppendixRowVersion));
        var termRv = DecodeRowVersion(request.RowVersion, nameof(request.RowVersion));
        await ExecuteInTransactionAsync(async () =>
        {
            var version = await GetVersionForMutationAsync(versionId, cancellationToken);
            EnsureDraft(version); MatchAndTrack(version, versionRv);
            var appendix = await GetAppendixForMutationAsync(versionId, appendixId, cancellationToken);
            MatchAndTrack(appendix, appendixRv);
            var row = await GetAppendixTermForMutationAsync(appendixId, termId, cancellationToken);
            MatchAndTrack(row, termRv);
            _dbContext.TblContractTemplateAppendixTerms.Remove(row);
            var now = DateTime.UtcNow; appendix.UpdatedEmployeeId = employeeId;
            appendix.UpdatedDate = now; RotateRowVersionIfNeeded(appendix);
            InvalidateAppendixPreview(version, employeeId, now);
            StageAppendixAudit(version, employeeId,
                ContractTemplateAuditActionTypes.TemplateAppendixTermDeleted,
                appendixId, null, termId);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }, cancellationToken);
    }

    public async Task<ContractTemplateAppendixResponse> ReorderAppendixTermsAsync(
        int versionId, int appendixId,
        ReorderContractTemplateAppendixTermsRequest request, int employeeId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await EnsureAdminOfficerAsync(employeeId, cancellationToken);
        ValidateReorder(request.Terms, x => x.TemplateAppendixTermId,
            x => x.DisplayOrder, "Appendix term");
        var versionRv = DecodeRowVersion(request.VersionRowVersion, nameof(request.VersionRowVersion));
        var appendixRv = DecodeRowVersion(request.AppendixRowVersion, nameof(request.AppendixRowVersion));
        return await ExecuteInTransactionAsync(async () =>
        {
            var version = await GetVersionForMutationAsync(versionId, cancellationToken);
            EnsureDraft(version); MatchAndTrack(version, versionRv);
            var appendix = await GetAppendixForMutationAsync(versionId, appendixId, cancellationToken);
            MatchAndTrack(appendix, appendixRv);
            var rows = await _dbContext.TblContractTemplateAppendixTerms
                .Where(x => x.TemplateAppendixId == appendixId).ToListAsync(cancellationToken);
            var byId = request.Terms.ToDictionary(x => x.TemplateAppendixTermId);
            if (!rows.Select(x => x.TemplateAppendixTermId).ToHashSet().SetEquals(byId.Keys))
                throw new ArgumentException("Danh sách reorder phải chứa đúng toàn bộ Appendix term.");
            foreach (var row in rows)
            {
                var item = byId[row.TemplateAppendixTermId];
                MatchAndTrack(row, DecodeRowVersion(item.RowVersion, nameof(item.RowVersion)));
                row.DisplayOrder = item.DisplayOrder; row.UpdatedEmployeeId = employeeId;
                row.UpdatedDate = DateTime.UtcNow; RotateRowVersionIfNeeded(row);
            }
            var now = DateTime.UtcNow; appendix.UpdatedEmployeeId = employeeId;
            appendix.UpdatedDate = now; RotateRowVersionIfNeeded(appendix);
            InvalidateAppendixPreview(version, employeeId, now);
            StageAppendixAudit(version, employeeId,
                ContractTemplateAuditActionTypes.TemplateAppendixTermsReordered,
                appendixId, rows.Count);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return await LoadAppendixAsync(versionId, appendixId, cancellationToken);
        }, cancellationToken);
    }

    private static (string Code, string Name, string? NameEn,
        string? Description, bool Required, bool SelectedByDefault, int Order)
        NormalizeAppendix(CreateContractTemplateAppendixRequest request)
    {
        var code = NormalizeRequired(request.AppendixCode, 50,
            nameof(request.AppendixCode)).ToUpperInvariant();
        if (!TemplateCodePattern.IsMatch(code))
            throw new ArgumentException("AppendixCode không đúng định dạng.");
        if (request.IsRequired && !request.IsSelectedByDefault)
            throw new ArgumentException("Appendix bắt buộc phải được chọn mặc định.");
        if (request.DisplayOrder < 0) throw new ArgumentException("DisplayOrder không được âm.");
        return (code, NormalizeRequired(request.AppendixName, 500,
            nameof(request.AppendixName)), NormalizeOptional(request.AppendixNameEn, 500),
            NormalizeOptional(request.AppendixDescription, 2000), request.IsRequired,
            request.IsSelectedByDefault, request.DisplayOrder);
    }

    private static (string Code, string Title, string? TitleEn,
        string? Content, string? ContentEn, int Order) NormalizeAppendixTerm(
        string? code, string? title, string? titleEn, string? content,
        string? contentEn, int order)
    {
        if (order < 0) throw new ArgumentException("DisplayOrder không được âm.");
        return (NormalizeRequired(code, 100, nameof(code)).ToUpperInvariant(),
            NormalizeRequired(title, 1000, nameof(title)),
            NormalizeOptional(titleEn, 1000), NormalizeRichText(content, nameof(content)),
            NormalizeRichText(contentEn, nameof(contentEn)), order);
    }

    private async Task EnsureAppendixUniqueAsync(int versionId, string code,
        int order, int? excludedId, CancellationToken ct)
    {
        if (await _dbContext.TblContractTemplateAppendices.AnyAsync(x =>
                x.TemplateVersionId == versionId
                && x.TemplateAppendixId != excludedId
                && (x.AppendixCode == code || x.DisplayOrder == order), ct))
            throw new ArgumentException("Mã và thứ tự Appendix phải duy nhất.");
    }

    private async Task EnsureAppendixTermUniqueAsync(int appendixId, string code,
        int order, int? excludedId, CancellationToken ct)
    {
        if (await _dbContext.TblContractTemplateAppendixTerms.AnyAsync(x =>
                x.TemplateAppendixId == appendixId
                && x.TemplateAppendixTermId != excludedId
                && (x.TermCode == code || x.DisplayOrder == order), ct))
            throw new ArgumentException("Mã và thứ tự Appendix term phải duy nhất.");
    }

    private async Task<TblContractTemplateAppendix> GetAppendixForMutationAsync(
        int versionId, int appendixId, CancellationToken ct) =>
        await _dbContext.TblContractTemplateAppendices.SingleOrDefaultAsync(x =>
            x.TemplateAppendixId == appendixId && x.TemplateVersionId == versionId, ct)
        ?? throw new KeyNotFoundException("Không tìm thấy Template Appendix.");

    private async Task<TblContractTemplateAppendixTerm> GetAppendixTermForMutationAsync(
        int appendixId, int termId, CancellationToken ct) =>
        await _dbContext.TblContractTemplateAppendixTerms.SingleOrDefaultAsync(x =>
            x.TemplateAppendixTermId == termId && x.TemplateAppendixId == appendixId, ct)
        ?? throw new KeyNotFoundException("Không tìm thấy Template Appendix term.");

    private async Task<ContractTemplateAppendixResponse> LoadAppendixAsync(
        int versionId, int appendixId, CancellationToken ct)
    {
        var row = await _dbContext.TblContractTemplateAppendices.AsNoTracking()
            .SingleOrDefaultAsync(x => x.TemplateAppendixId == appendixId
                && x.TemplateVersionId == versionId, ct)
            ?? throw new KeyNotFoundException("Không tìm thấy Template Appendix.");
        var terms = await _dbContext.TblContractTemplateAppendixTerms.AsNoTracking()
            .Where(x => x.TemplateAppendixId == appendixId)
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.TemplateAppendixTermId)
            .ToListAsync(ct);
        var result = MapAppendix(row);
        result.Terms = terms.Select(MapAppendixTerm).ToList();
        return result;
    }

    private static ContractTemplateAppendixResponse MapAppendix(
        TblContractTemplateAppendix row) => new()
        {
            TemplateAppendixId = row.TemplateAppendixId,
            TemplateVersionId = row.TemplateVersionId,
            AppendixCode = row.AppendixCode, AppendixName = row.AppendixName,
            AppendixNameEn = row.AppendixNameEn,
            AppendixDescription = row.AppendixDescription,
            IsRequired = row.IsRequired,
            IsSelectedByDefault = row.IsSelectedByDefault,
            DisplayOrder = row.DisplayOrder,
            CreatedEmployeeId = row.CreatedEmployeeId, CreatedDate = row.CreatedDate,
            UpdatedEmployeeId = row.UpdatedEmployeeId, UpdatedDate = row.UpdatedDate,
            RowVersion = EncodeRowVersion(row.RowVersion)
        };

    private static ContractTemplateAppendixTermResponse MapAppendixTerm(
        TblContractTemplateAppendixTerm row) => new()
        {
            TemplateAppendixTermId = row.TemplateAppendixTermId,
            TemplateAppendixId = row.TemplateAppendixId, TermCode = row.TermCode,
            TermTitle = row.TermTitle, TermTitleEn = row.TermTitleEn,
            TermContent = row.TermContent, TermContentEn = row.TermContentEn,
            DisplayOrder = row.DisplayOrder,
            CreatedEmployeeId = row.CreatedEmployeeId, CreatedDate = row.CreatedDate,
            UpdatedEmployeeId = row.UpdatedEmployeeId, UpdatedDate = row.UpdatedDate,
            RowVersion = EncodeRowVersion(row.RowVersion)
        };

    private void MatchAndTrack(TblContractTemplateVersion row, byte[] expected)
    { EnsureRowVersionMatches(row.RowVersion, expected, "Template version"); SetOriginalRowVersion(row, expected); }
    private void MatchAndTrack(TblContractTemplateAppendix row, byte[] expected)
    { EnsureRowVersionMatches(row.RowVersion, expected, "Template Appendix"); _dbContext.Entry(row).Property(x => x.RowVersion).OriginalValue = expected; }
    private void MatchAndTrack(TblContractTemplateAppendixTerm row, byte[] expected)
    { EnsureRowVersionMatches(row.RowVersion, expected, "Template Appendix term"); _dbContext.Entry(row).Property(x => x.RowVersion).OriginalValue = expected; }

    private void InvalidateAppendixPreview(TblContractTemplateVersion version,
        int employeeId, DateTime now)
    {
        version.PreviewFileId = null;
        TouchVersion(version, employeeId, now);
        RotateVersionRowVersionIfNeeded(version);
    }

    private void SetSyntheticRowVersionIfNeeded(TblContractTemplateAppendix row)
    { if (IsInMemoryProvider() && row.RowVersion is not { Length: 8 }) row.RowVersion = NewSyntheticRowVersion(); }
    private void SetSyntheticRowVersionIfNeeded(TblContractTemplateAppendixTerm row)
    { if (IsInMemoryProvider() && row.RowVersion is not { Length: 8 }) row.RowVersion = NewSyntheticRowVersion(); }
    private void RotateRowVersionIfNeeded(TblContractTemplateAppendix row)
    { if (IsInMemoryProvider()) row.RowVersion = NewSyntheticRowVersion(); }
    private void RotateRowVersionIfNeeded(TblContractTemplateAppendixTerm row)
    { if (IsInMemoryProvider()) row.RowVersion = NewSyntheticRowVersion(); }

    private static void ValidateReorder<T>(IReadOnlyCollection<T>? rows,
        Func<T, int> id, Func<T, int> order, string name)
    {
        if (rows is null) throw new ArgumentException($"Danh sách {name} không được null.");
        if (rows.Any(x => id(x) <= 0 || order(x) < 0)
            || rows.Select(id).Distinct().Count() != rows.Count
            || rows.Select(order).Distinct().Count() != rows.Count)
            throw new ArgumentException($"ID và DisplayOrder của {name} phải hợp lệ và duy nhất.");
    }

    private void StageAppendixAudit(TblContractTemplateVersion version,
        int employeeId, string action, int? appendixId, int? termCount,
        int? termId = null)
    {
        if (_templateAuditWriter is null) return;
        var values = new List<(string Key, object? Value)>();
        if (appendixId.HasValue) values.Add(("TemplateAppendixId", appendixId.Value));
        if (termId.HasValue) values.Add(("TemplateAppendixTermId", termId.Value));
        if (termCount.HasValue) values.Add(("AppendixTermCount", termCount.Value));
        _templateAuditWriter.StageAudits([new ContractTemplateAuditWriteRequest(
            version.TemplateId, version.TemplateVersionId, employeeId, action,
            ContractTemplateAuditResults.Succeeded, DateTime.UtcNow,
            NewValues: ContractTemplateAuditValues.Create(values.ToArray()))]);
    }

    private async Task<List<ContractTemplateAppendixResponse>> LoadAppendicesAsync(
        int versionId, CancellationToken ct)
    {
        var appendices = await _dbContext.TblContractTemplateAppendices
            .AsNoTracking().Where(x => x.TemplateVersionId == versionId)
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.TemplateAppendixId)
            .ToListAsync(ct);
        var ids = appendices.Select(x => x.TemplateAppendixId).ToList();
        var terms = await _dbContext.TblContractTemplateAppendixTerms
            .AsNoTracking().Where(x => ids.Contains(x.TemplateAppendixId))
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.TemplateAppendixTermId)
            .ToListAsync(ct);
        return appendices.Select(appendix =>
        {
            var response = MapAppendix(appendix);
            response.Terms = terms.Where(x => x.TemplateAppendixId == appendix.TemplateAppendixId)
                .Select(MapAppendixTerm).ToList();
            return response;
        }).ToList();
    }

    private async Task ValidateAppendicesForPublishAsync(int versionId,
        ContractLanguageMode languageMode, CancellationToken ct)
    {
        var appendices = await _dbContext.TblContractTemplateAppendices
            .AsNoTracking().Where(x => x.TemplateVersionId == versionId)
            .OrderBy(x => x.DisplayOrder).ToListAsync(ct);
        if (appendices.Count == 0) return;
        if (appendices.Any(x => x.IsRequired && !x.IsSelectedByDefault))
            throw new ContractTemplatePreviewException("AppendixFlagsInvalid",
                "Appendix bắt buộc phải được chọn mặc định.");
        var hasMarker = await _dbContext.TblContractTemplateFields.AsNoTracking()
            .AnyAsync(x => x.TemplateVersionId == versionId
                && x.PlaceholderKey == "CONTRACT_APPENDICES", ct);
        if (!hasMarker)
            throw new ContractTemplatePreviewException("AppendixMarkerMissing",
                "DOCX phải có marker CONTRACT_APPENDICES khi template có Appendix.");
        var ids = appendices.Select(x => x.TemplateAppendixId).ToList();
        var terms = await _dbContext.TblContractTemplateAppendixTerms.AsNoTracking()
            .Where(x => ids.Contains(x.TemplateAppendixId)).ToListAsync(ct);
        foreach (var appendix in appendices)
        {
            var owned = terms.Where(x => x.TemplateAppendixId == appendix.TemplateAppendixId)
                .ToList();
            if (owned.Count == 0 || owned.Any(x => string.IsNullOrWhiteSpace(x.TermContent))
                || (languageMode == ContractLanguageMode.Bilingual
                    && owned.Any(x => string.IsNullOrWhiteSpace(x.TermTitleEn)
                        || string.IsNullOrWhiteSpace(x.TermContentEn))))
                throw new ContractTemplatePreviewException("AppendixTermsInvalid",
                    $"Appendix {appendix.AppendixCode} phải có ít nhất một điều khoản đầy đủ.");
        }

        // Renderer package đã nhận toàn bộ Appendix relational data. Không có
        // nhánh fallback hoặc renderer cũ cho dữ liệu thiếu cấu trúc.
    }
}
