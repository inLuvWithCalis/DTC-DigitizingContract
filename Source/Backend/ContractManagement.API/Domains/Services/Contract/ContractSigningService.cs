using System.Data;
using ContractManagement.API.Common.Enums;
using ContractManagement.API.Common.Exceptions;
using ContractManagement.API.Domains.DTOs.Requests.Contract;
using ContractManagement.API.Domains.DTOs.Responses.Contract;
using ContractManagement.API.Domains.Policies.Contract;
using ContractManagement.Domains.Interfaces.Contract;
using ContractManagement.Domains.Interfaces.File;
using ContractManagement.Domains.Services.File;
using ContractManagement.Infrastructure.MultiTenancy.Interfaces;
using ContractManagement.Infrastructure.Persistence.Application;
using ContractManagement.Infrastructure.Persistence.Application.Models;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Domains.Services.Contract;

public static class ContractSigningErrorCodes
{
    public const string SigningStateChanged = "SigningStateChanged";
    public const string ApprovedArtifactMissing = "ApprovedArtifactMissing";
    public const string ActiveEvidenceExists = "ActiveEvidenceExists";
    public const string SupersedeReasonRequired = "SupersedeReasonRequired";
    public const string SignatureMetadataInvalid = "SignatureMetadataInvalid";
}

/// <summary>
/// Phase 9 wet-ink workflow. One evidence file proves both signatures and is
/// append-only: replacing it creates a new record and supersedes the old one.
/// </summary>
public sealed class ContractSigningService : IContractSigningService
{
    private const string EvidenceObjectType = "ContractSignedEvidence";
    private const string ApprovedArtifactObjectType = "ContractVersionArtifact";

    private readonly DbDtctechContext _dbContext;
    private readonly IContractResourceAuthorizationService _authorization;
    private readonly IContractAuditWriter _auditWriter;
    private readonly IPrivateFileStorage _privateFileStorage;
    private readonly ICurrentTenant _currentTenant;

    public ContractSigningService(
        DbDtctechContext dbContext,
        IContractResourceAuthorizationService authorization,
        IContractAuditWriter auditWriter,
        IPrivateFileStorage privateFileStorage,
        ICurrentTenant currentTenant)
    {
        _dbContext = dbContext;
        _authorization = authorization;
        _auditWriter = auditWriter;
        _privateFileStorage = privateFileStorage;
        _currentTenant = currentTenant;
    }

    public async Task<ContractSigningDetailResponse> GetAsync(
        int contractId,
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        await _authorization.EnsureCanReadAsync(
            contractId,
            employeeId,
            cancellationToken);

        var contract = await _dbContext.TblContracts
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.ContractId == contractId,
                cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy hợp đồng.");
        if (!contract.CurrentVersionId.HasValue)
        {
            throw new InvalidOperationException(
                "Hợp đồng chưa có version hiện hành.");
        }

        var version = await _dbContext.TblContractVersions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.VersionId == contract.CurrentVersionId
                    && candidate.ContractId == contract.ContractId,
                cancellationToken)
            ?? throw new KeyNotFoundException(
                "Không tìm thấy version hiện hành của hợp đồng.");
        var artifacts = await LoadApprovedArtifactsAsync(
            contract.ContractId,
            version.VersionId,
            cancellationToken);
        var evidence = await LoadEvidenceAsync(
            contract.ContractId,
            cancellationToken);

        return new ContractSigningDetailResponse
        {
            ContractId = contract.ContractId,
            ContractStatus = (ContractStatus)contract.Status,
            VersionId = version.VersionId,
            VersionNo = version.VersionNo,
            VersionLocked = version.IsLocked,
            ContractRowVersion = EncodeRowVersion(contract.RowVersion),
            VersionRowVersion = EncodeRowVersion(version.RowVersion),
            ApprovedArtifacts = artifacts,
            ActiveEvidence = evidence.FirstOrDefault(item =>
                item.Status == SignedEvidenceStatus.Active
                && item.VersionId == version.VersionId),
            EvidenceHistory = evidence
        };
    }

    public Task<ContractSignedEvidenceResponse> UploadAsync(
        int contractId,
        UploadContractSignedEvidenceRequest request,
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateMetadata(request);
        return SaveAsync(
            contractId,
            null,
            request,
            null,
            null,
            employeeId,
            cancellationToken);
    }

    public Task<ContractSignedEvidenceResponse> SupersedeAsync(
        int contractId,
        int signedEvidenceId,
        SupersedeContractSignedEvidenceRequest request,
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateMetadata(request);
        var reason = NormalizeRequired(
            request.Reason,
            1000,
            ContractSigningErrorCodes.SupersedeReasonRequired,
            "Thay bản scan bắt buộc phải nhập lý do.");
        return SaveAsync(
            contractId,
            signedEvidenceId,
            request,
            request.EvidenceRowVersion,
            reason,
            employeeId,
            cancellationToken);
    }

    private async Task<ContractSignedEvidenceResponse> SaveAsync(
        int contractId,
        int? supersededEvidenceId,
        UploadContractSignedEvidenceRequest request,
        string? evidenceRowVersion,
        string? supersedeReason,
        int employeeId,
        CancellationToken cancellationToken)
    {
        if (contractId <= 0 || request.CurrentVersionId <= 0)
        {
            throw new ArgumentException("ContractId và CurrentVersionId phải lớn hơn 0.");
        }

        await _authorization.EnsureCanWriteAsync(
            contractId,
            employeeId,
            cancellationToken);
        var expectedContractRowVersion = DecodeRowVersion(
            request.ContractRowVersion,
            nameof(request.ContractRowVersion));
        var expectedVersionRowVersion = DecodeRowVersion(
            request.VersionRowVersion,
            nameof(request.VersionRowVersion));
        var expectedEvidenceRowVersion = supersededEvidenceId.HasValue
            ? DecodeRowVersion(
                evidenceRowVersion ?? string.Empty,
                nameof(SupersedeContractSignedEvidenceRequest
                    .EvidenceRowVersion))
            : null;
        var strategy = _dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            StoredPrivateFile? storedFile = null;
            await using var transaction = await _dbContext.Database
                .BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken);
            try
            {
                var contract = await _dbContext.TblContracts
                    .SingleOrDefaultAsync(
                        candidate => candidate.ContractId == contractId,
                        cancellationToken)
                    ?? throw new KeyNotFoundException("Không tìm thấy hợp đồng.");
                if (contract.EmployeeId != employeeId)
                {
                    throw new KeyNotFoundException("Không tìm thấy hợp đồng.");
                }

                EnsureRowVersionMatches(
                    contract.RowVersion,
                    expectedContractRowVersion,
                    "Hợp đồng");
                _dbContext.Entry(contract)
                    .Property(candidate => candidate.RowVersion)
                    .OriginalValue = expectedContractRowVersion;

                var version = await _dbContext.TblContractVersions
                    .SingleOrDefaultAsync(
                        candidate => candidate.VersionId ==
                                request.CurrentVersionId
                            && candidate.ContractId == contract.ContractId,
                        cancellationToken)
                    ?? throw Rule(
                        StatusCodes.Status409Conflict,
                        ContractSigningErrorCodes.SigningStateChanged,
                        "Version ký không còn là version của hợp đồng.");
                EnsureRowVersionMatches(
                    version.RowVersion,
                    expectedVersionRowVersion,
                    "Version hợp đồng");

                var artifacts = await LoadApprovedArtifactsAsync(
                    contract.ContractId,
                    version.VersionId,
                    cancellationToken);
                var approvedArtifactsExist = HasCompleteApprovedArtifacts(
                    artifacts);
                var activeEvidence = await _dbContext
                    .TblContractSignedEvidences
                    .SingleOrDefaultAsync(
                        candidate => candidate.ContractId == contract.ContractId
                            && candidate.VersionId == version.VersionId
                            && candidate.Status ==
                                (byte)SignedEvidenceStatus.Active,
                        cancellationToken);

                try
                {
                    if (supersededEvidenceId.HasValue)
                    {
                        if (activeEvidence?.SignedEvidenceId !=
                            supersededEvidenceId.Value)
                        {
                            throw new InvalidOperationException(
                                "Bản scan đang hiệu lực đã thay đổi.");
                        }

                        EnsureRowVersionMatches(
                            activeEvidence.RowVersion,
                            expectedEvidenceRowVersion!,
                            "Bản scan đã ký");
                        _dbContext.Entry(activeEvidence)
                            .Property(candidate => candidate.RowVersion)
                            .OriginalValue = expectedEvidenceRowVersion!;
                        SignaturePolicy.EnsureCanSupersedeEvidence(
                            (ContractStatus)contract.Status,
                            contract.CurrentVersionId ?? 0,
                            version.VersionId,
                            version.IsLocked,
                            approvedArtifactsExist,
                            activeEvidenceExists: true);
                    }
                    else
                    {
                        SignaturePolicy.EnsureCanUploadInitialEvidence(
                            (ContractStatus)contract.Status,
                            contract.CurrentVersionId ?? 0,
                            version.VersionId,
                            version.IsLocked,
                            approvedArtifactsExist,
                            activeEvidence is not null);
                    }
                }
                catch (InvalidOperationException exception)
                {
                    var code = !approvedArtifactsExist
                        ? ContractSigningErrorCodes.ApprovedArtifactMissing
                        : activeEvidence is not null && !supersededEvidenceId.HasValue
                            ? ContractSigningErrorCodes.ActiveEvidenceExists
                            : ContractSigningErrorCodes.SigningStateChanged;
                    throw Rule(
                        StatusCodes.Status409Conflict,
                        code,
                        exception.Message);
                }

                var tenant = _currentTenant.GetRequiredTenant();
                await using var content = request.File.OpenReadStream();
                storedFile = await _privateFileStorage.SaveAsync(
                    new PrivateFileSaveRequest(
                        content,
                        request.File.FileName,
                        request.File.ContentType,
                        request.File.Length,
                        tenant.TenantCode,
                        EvidenceObjectType,
                        contract.ContractId,
                        PrivateFileUploadPolicies.ContractEvidence()),
                    cancellationToken);

                var fileMetadata = CreateFileMetadata(
                    storedFile,
                    contract.ContractId,
                    employeeId);
                _dbContext.TblFileStorages.Add(fileMetadata);
                await _dbContext.SaveChangesAsync(cancellationToken);

                var now = DateTime.UtcNow;
                var previousContractStatus = contract.Status;
                var previousEvidenceFileId = activeEvidence?.FileId;
                var previousEvidenceId = activeEvidence?.SignedEvidenceId;
                if (activeEvidence is not null)
                {
                    activeEvidence.Status =
                        (byte)SignedEvidenceStatus.Superseded;
                    activeEvidence.SupersedeReason = supersedeReason;
                    activeEvidence.SupersededByEmployeeId = employeeId;
                    activeEvidence.SupersededAt = now;
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }

                var evidence = new TblContractSignedEvidence
                {
                    ContractId = contract.ContractId,
                    VersionId = version.VersionId,
                    FileId = fileMetadata.FileId,
                    Status = (byte)SignedEvidenceStatus.Active,
                    ProviderSignerName = NormalizeMetadata(
                        request.ProviderSignerName),
                    ProviderSignerTitle = NormalizeMetadata(
                        request.ProviderSignerTitle),
                    ProviderSigningDate = request.ProviderSigningDate.Date,
                    CustomerSignerName = NormalizeMetadata(
                        request.CustomerSignerName),
                    CustomerSignerTitle = NormalizeMetadata(
                        request.CustomerSignerTitle),
                    CustomerSigningDate = request.CustomerSigningDate.Date,
                    SupersedesEvidenceId = activeEvidence?.SignedEvidenceId,
                    UploadedByEmployeeId = employeeId,
                    UploadedAt = now
                };
                _dbContext.TblContractSignedEvidences.Add(evidence);

                if (!supersededEvidenceId.HasValue)
                {
                    contract.Status = (byte)ContractStatus.Signed;
                }

                contract.SignDate = request.ProviderSigningDate.Date >
                    request.CustomerSigningDate.Date
                    ? request.ProviderSigningDate.Date
                    : request.CustomerSigningDate.Date;
                contract.UpdatedEmployeeId = employeeId;
                contract.UpdateDate = now;
                await _dbContext.SaveChangesAsync(cancellationToken);

                _auditWriter.StageEmployeeAudits(
                [
                    new EmployeeContractAuditWriteRequest(
                        contract.ContractId,
                        version.VersionId,
                        employeeId,
                        supersededEvidenceId.HasValue
                            ? ContractAuditActionTypes
                                .SignedEvidenceSuperseded
                            : ContractAuditActionTypes.SignedEvidenceUploaded,
                        ContractAuditResults.Succeeded,
                        now,
                        PreviousContractStatus: previousContractStatus,
                        NewContractStatus: contract.Status,
                        Reason: supersedeReason,
                        SubjectType:
                            ContractAuditSubjectTypes.SignedEvidence,
                        SubjectId: evidence.SignedEvidenceId,
                        PreviousValues: activeEvidence is null
                            ? null
                            : ContractAuditValues.Create(
                                ("SignedEvidenceId", previousEvidenceId),
                                ("FileId", previousEvidenceFileId),
                                ("EvidenceStatus",
                                    (byte)SignedEvidenceStatus.Active)),
                        NewValues: ContractAuditValues.Create(
                            ("Status", contract.Status),
                            ("CurrentVersionId", version.VersionId),
                            ("SignedEvidenceId", evidence.SignedEvidenceId),
                            ("FileId", fileMetadata.FileId),
                            ("FileType", fileMetadata.FileType),
                            ("Sha256", fileMetadata.Sha256),
                            ("EvidenceStatus", evidence.Status),
                            ("SupersedesEvidenceId",
                                evidence.SupersedesEvidenceId),
                            ("ProviderSigningDate",
                                evidence.ProviderSigningDate),
                            ("CustomerSigningDate",
                                evidence.CustomerSigningDate)))
                ]);
                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return await LoadEvidenceByIdAsync(
                    evidence.SignedEvidenceId,
                    cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                _dbContext.ChangeTracker.Clear();
                if (storedFile is not null)
                {
                    await _privateFileStorage.DeleteAsync(
                        storedFile.TenantCode,
                        storedFile.StorageKey,
                        cancellationToken);
                }

                throw;
            }
        });
    }

    private async Task<IReadOnlyList<ContractSigningArtifactResponse>>
        LoadApprovedArtifactsAsync(
            int contractId,
            int versionId,
            CancellationToken cancellationToken)
    {
        var approved = await _dbContext.TblContractApprovalRequests
            .AsNoTracking()
            .AnyAsync(
                request => request.ContractId == contractId
                    && request.VersionId == versionId
                    && request.Status ==
                        (byte)ApprovalRequestStatus.Approved,
                cancellationToken);
        if (!approved)
        {
            return [];
        }

        return await _dbContext.TblFileStorages
            .AsNoTracking()
            .Where(file => file.ObjectType == ApprovedArtifactObjectType
                && file.ObjectId == versionId)
            .OrderBy(file => file.FileType)
            .Select(file => new ContractSigningArtifactResponse
            {
                FileId = file.FileId,
                FileName = file.FileName,
                FileType = file.FileType ?? string.Empty,
                ContentType = file.ContentType ?? string.Empty,
                FileSize = file.FileSize ?? 0,
                Sha256 = file.Sha256 ?? string.Empty
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<ContractSignedEvidenceResponse>>
        LoadEvidenceAsync(
            int contractId,
            CancellationToken cancellationToken)
    {
        var ids = await _dbContext.TblContractSignedEvidences
            .AsNoTracking()
            .Where(evidence => evidence.ContractId == contractId)
            .OrderByDescending(evidence => evidence.UploadedAt)
            .ThenByDescending(evidence => evidence.SignedEvidenceId)
            .Select(evidence => evidence.SignedEvidenceId)
            .ToListAsync(cancellationToken);
        var result = new List<ContractSignedEvidenceResponse>(ids.Count);
        foreach (var id in ids)
        {
            result.Add(await LoadEvidenceByIdAsync(id, cancellationToken));
        }

        return result;
    }

    private async Task<ContractSignedEvidenceResponse> LoadEvidenceByIdAsync(
        int signedEvidenceId,
        CancellationToken cancellationToken)
    {
        var row = await (
            from evidence in _dbContext.TblContractSignedEvidences.AsNoTracking()
            join file in _dbContext.TblFileStorages.AsNoTracking()
                on evidence.FileId equals file.FileId
            join version in _dbContext.TblContractVersions.AsNoTracking()
                on evidence.VersionId equals version.VersionId
            join uploader in _dbContext.TblEmployees.AsNoTracking()
                on evidence.UploadedByEmployeeId equals uploader.EmployeeId
            join supersederCandidate in _dbContext.TblEmployees.AsNoTracking()
                on evidence.SupersededByEmployeeId equals
                    (int?)supersederCandidate.EmployeeId into supersederRows
            from superseder in supersederRows.DefaultIfEmpty()
            where evidence.SignedEvidenceId == signedEvidenceId
            select new
            {
                Evidence = evidence,
                File = file,
                VersionNo = version.VersionNo,
                UploaderName = uploader.EmployeeFullName,
                SupersederName = superseder == null
                    ? null
                    : superseder.EmployeeFullName
            }).SingleOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException(
                "Không tìm thấy bản scan hợp đồng đã ký.");

        return new ContractSignedEvidenceResponse
        {
            SignedEvidenceId = row.Evidence.SignedEvidenceId,
            ContractId = row.Evidence.ContractId,
            VersionId = row.Evidence.VersionId,
            VersionNo = row.VersionNo,
            FileId = row.File.FileId,
            FileName = row.File.FileName,
            FileType = row.File.FileType ?? string.Empty,
            ContentType = row.File.ContentType ?? string.Empty,
            FileSize = row.File.FileSize ?? 0,
            Sha256 = row.File.Sha256 ?? string.Empty,
            Status = (SignedEvidenceStatus)row.Evidence.Status,
            ProviderSignerName = row.Evidence.ProviderSignerName,
            ProviderSignerTitle = row.Evidence.ProviderSignerTitle,
            ProviderSigningDate = row.Evidence.ProviderSigningDate,
            CustomerSignerName = row.Evidence.CustomerSignerName,
            CustomerSignerTitle = row.Evidence.CustomerSignerTitle,
            CustomerSigningDate = row.Evidence.CustomerSigningDate,
            SupersedesEvidenceId = row.Evidence.SupersedesEvidenceId,
            SupersedeReason = row.Evidence.SupersedeReason,
            UploadedByEmployeeId = row.Evidence.UploadedByEmployeeId,
            UploadedByEmployeeName = row.UploaderName,
            UploadedAt = row.Evidence.UploadedAt,
            SupersededByEmployeeId = row.Evidence.SupersededByEmployeeId,
            SupersededByEmployeeName = row.SupersederName,
            SupersededAt = row.Evidence.SupersededAt,
            RowVersion = EncodeRowVersion(row.Evidence.RowVersion)
        };
    }

    private static bool HasCompleteApprovedArtifacts(
        IReadOnlyList<ContractSigningArtifactResponse> artifacts) =>
        artifacts.Count == 2
        && artifacts.Count(artifact => string.Equals(
            artifact.FileType,
            "docx",
            StringComparison.OrdinalIgnoreCase)) == 1
        && artifacts.Count(artifact => string.Equals(
            artifact.FileType,
            "pdf",
            StringComparison.OrdinalIgnoreCase)) == 1
        && artifacts.All(artifact =>
            artifact.FileId > 0
            && !string.IsNullOrWhiteSpace(artifact.Sha256));

    private static TblFileStorage CreateFileMetadata(
        StoredPrivateFile stored,
        int contractId,
        int employeeId) => new()
        {
            ObjectType = EvidenceObjectType,
            ObjectId = contractId,
            FileName = stored.OriginalFileName,
            FilePath = string.Empty,
            StorageKey = stored.StorageKey,
            ContentType = stored.ContentType,
            Sha256 = stored.Sha256,
            TenantCode = stored.TenantCode,
            FileType = Path.GetExtension(stored.OriginalFileName)
                .TrimStart('.')
                .ToLowerInvariant(),
            FileSize = stored.FileSize,
            UploadedByUserId = employeeId,
            UploadedDate = stored.CreatedAt
        };

    private static void ValidateMetadata(
        UploadContractSignedEvidenceRequest request)
    {
        if (request.File is null || request.File.Length <= 0)
        {
            throw Rule(
                StatusCodes.Status400BadRequest,
                ContractSigningErrorCodes.SignatureMetadataInvalid,
                "Vui lòng chọn file scan đã ký.");
        }

        _ = NormalizeRequired(
            request.ProviderSignerName,
            200,
            ContractSigningErrorCodes.SignatureMetadataInvalid,
            "Tên người ký phía nhà cung cấp là bắt buộc.");
        _ = NormalizeRequired(
            request.ProviderSignerTitle,
            200,
            ContractSigningErrorCodes.SignatureMetadataInvalid,
            "Chức danh người ký phía nhà cung cấp là bắt buộc.");
        _ = NormalizeRequired(
            request.CustomerSignerName,
            200,
            ContractSigningErrorCodes.SignatureMetadataInvalid,
            "Tên người ký phía khách hàng là bắt buộc.");
        _ = NormalizeRequired(
            request.CustomerSignerTitle,
            200,
            ContractSigningErrorCodes.SignatureMetadataInvalid,
            "Chức danh người ký phía khách hàng là bắt buộc.");
        if (request.ProviderSigningDate == default
            || request.CustomerSigningDate == default)
        {
            throw Rule(
                StatusCodes.Status400BadRequest,
                ContractSigningErrorCodes.SignatureMetadataInvalid,
                "Ngày ký của hai bên là bắt buộc.");
        }
    }

    private static string NormalizeMetadata(string value) => value.Trim();

    private static string NormalizeRequired(
        string value,
        int maxLength,
        string errorCode,
        string message)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw Rule(StatusCodes.Status400BadRequest, errorCode, message);
        }

        if (normalized.Length > maxLength)
        {
            throw Rule(
                StatusCodes.Status400BadRequest,
                errorCode,
                $"Dữ liệu không được vượt quá {maxLength} ký tự.");
        }

        return normalized;
    }

    private static byte[] DecodeRowVersion(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{fieldName} không được để trống.");
        }

        try
        {
            var bytes = Convert.FromBase64String(value);
            return bytes.Length > 0 ? bytes : throw new FormatException();
        }
        catch (FormatException)
        {
            throw new ArgumentException(
                $"{fieldName} không đúng định dạng Base64.");
        }
    }

    private static void EnsureRowVersionMatches(
        byte[]? current,
        byte[] expected,
        string resourceName)
    {
        if (current is null || !current.AsSpan().SequenceEqual(expected))
        {
            throw new DbUpdateConcurrencyException(
                $"{resourceName} đã được cập nhật bởi request khác.");
        }
    }

    private static string EncodeRowVersion(byte[]? value) =>
        Convert.ToBase64String(value ?? []);

    private static BusinessRuleException Rule(
        int statusCode,
        string code,
        string message) => new(statusCode, code, message);
}
