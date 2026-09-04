using System.Data;
using System.Security.Cryptography;
using ContractManagement.API.Common.Enums;
using ContractManagement.API.Common.Exceptions;
using ContractManagement.API.Common.Responses;
using ContractManagement.API.Common.Security;
using ContractManagement.API.Domains.DTOs.Requests.Contract;
using ContractManagement.API.Domains.DTOs.Responses.Contract;
using ContractManagement.API.Domains.Policies.Contract;
using ContractManagement.Domains.Interfaces.Contract;
using ContractManagement.Domains.Interfaces.File;
using ContractManagement.Infrastructure.Persistence.Application;
using ContractManagement.Infrastructure.Persistence.Application.Models;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Domains.Services.Contract;

public static class ContractApprovalErrorCodes
{
    public const string ApprovalRequestAlreadyResolved =
        "ApprovalRequestAlreadyResolved";
    public const string ApprovalStateChanged = "ApprovalStateChanged";
    public const string ApprovalReasonRequired = "ApprovalReasonRequired";
    public const string SelfApprovalDenied = "SelfApprovalDenied";
    public const string ApprovalArtifactMissing = "ApprovalArtifactMissing";
}

/// <summary>
/// Phase 8D workflow. A submitted version and its artifacts remain immutable;
/// this service only resolves the approval request and moves Contract state.
/// </summary>
public sealed class ContractApprovalService : IContractApprovalService
{
    private const byte ActiveEmployeeStatus = 1;
    private const string ApprovalHistoryObjectType =
        "ContractApprovalRequest";
    private const string SubmittedArtifactObjectType =
        "ContractVersionArtifact";

    private readonly DbDtctechContext _dbContext;
    private readonly IContractResourceAuthorizationService _authorization;
    private readonly IContractAuditWriter _auditWriter;
    private readonly IPrivateFileStorage _privateFileStorage;

    public ContractApprovalService(
        DbDtctechContext dbContext,
        IContractResourceAuthorizationService authorization,
        IContractAuditWriter auditWriter,
        IPrivateFileStorage privateFileStorage)
    {
        _dbContext = dbContext;
        _authorization = authorization;
        _auditWriter = auditWriter;
        _privateFileStorage = privateFileStorage;
    }

    public async Task<PagedResult<ContractApprovalRequestResponse>>
        GetInboxAsync(
            ContractApprovalInboxFilterRequest filter,
            int managerEmployeeId,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        await EnsureManagerAsync(managerEmployeeId, cancellationToken);

        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);
        var keyword = filter.Keyword?.Trim();

        var allowedWorkflowIds = await _dbContext.TblApprovalWorkflows
            .AsNoTracking()
            .Where(workflow =>
                workflow.IsActive
                && workflow.ObjectType == "Contract"
                && workflow.StepNo == 1
                && (!workflow.ApproverEmployeeId.HasValue
                    || workflow.ApproverEmployeeId.Value == managerEmployeeId))
            .Select(workflow => workflow.WorkflowId)
            .ToListAsync(cancellationToken);

        var requestQuery = _dbContext.TblContractApprovalRequests
            .AsNoTracking()
            .Where(request =>
                request.Status == (byte)ApprovalRequestStatus.Pending
                && request.SubmittedByEmployeeId != managerEmployeeId
                && (!request.WorkflowId.HasValue
                    || allowedWorkflowIds.Contains(request.WorkflowId.Value)));
        var query =
            from request in requestQuery
            join contract in _dbContext.TblContracts.AsNoTracking()
                on request.ContractId equals contract.ContractId
            join version in _dbContext.TblContractVersions.AsNoTracking()
                on request.VersionId equals version.VersionId
            join submitter in _dbContext.TblEmployees.AsNoTracking()
                on request.SubmittedByEmployeeId equals submitter.EmployeeId
            join owner in _dbContext.TblEmployees.AsNoTracking()
                on contract.EmployeeId equals owner.EmployeeId
            join resolverCandidate in _dbContext.TblEmployees.AsNoTracking()
                on request.ResolvedByEmployeeId equals
                    (int?)resolverCandidate.EmployeeId into resolverRows
            from resolver in resolverRows.DefaultIfEmpty()
            select new
            {
                Request = request,
                Contract = contract,
                Version = version,
                Submitter = submitter,
                Owner = owner,
                Resolver = resolver
            };

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(row =>
                (row.Contract.ContractCode != null
                    && row.Contract.ContractCode.Contains(keyword))
                || row.Contract.ContractName.Contains(keyword)
                || (row.Owner.EmployeeFullName != null
                    && row.Owner.EmployeeFullName.Contains(keyword))
                || (row.Submitter.EmployeeFullName != null
                    && row.Submitter.EmployeeFullName.Contains(keyword)));
        }

        if (filter.FromDate.HasValue
            && filter.ToDate.HasValue
            && filter.FromDate.Value.Date > filter.ToDate.Value.Date)
        {
            throw new ArgumentException(
                "Từ ngày không được lớn hơn đến ngày.");
        }

        if (filter.FromDate.HasValue)
        {
            var fromDate = filter.FromDate.Value.Date;
            query = query.Where(row =>
                row.Request.SubmittedDate >= fromDate);
        }

        if (filter.ToDate.HasValue)
        {
            var toDate = filter.ToDate.Value.Date;
            query = toDate < DateTime.MaxValue.Date
                ? query.Where(row =>
                    row.Request.SubmittedDate < toDate.AddDays(1))
                : query.Where(row =>
                    row.Request.SubmittedDate <= toDate);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var rawRows = await query
            .OrderBy(row => row.Request.SubmittedDate)
            .ThenBy(row => row.Request.ApprovalRequestId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        var rows = rawRows.Select(row => new ApprovalRow(
            row.Request,
            row.Contract,
            row.Version,
            row.Submitter,
            row.Owner,
            row.Resolver));

        return new PagedResult<ContractApprovalRequestResponse>
        {
            Items = rows.Select(MapResponse).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ContractApprovalDetailResponse> GetDetailAsync(
        int approvalRequestId,
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        var approvalQuery = _dbContext.TblContractApprovalRequests
            .AsNoTracking()
            .Where(candidate => candidate.ApprovalRequestId ==
                approvalRequestId);
        var row = await ApprovalRows(approvalQuery)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException(
                "Không tìm thấy yêu cầu duyệt.");

        await _authorization.EnsureCanReadAsync(
            row.Contract.ContractId,
            employeeId,
            cancellationToken);

        var response = MapDetail(row);
        response.Artifacts = await LoadArtifactsAsync(
            row.Version.VersionId,
            cancellationToken);
        return response;
    }

    public async Task<IReadOnlyList<ContractApprovalRequestResponse>>
        GetContractHistoryAsync(
            int contractId,
            int employeeId,
            CancellationToken cancellationToken = default)
    {
        if (contractId <= 0)
        {
            throw new ArgumentException("ContractId phải lớn hơn 0.");
        }

        await _authorization.EnsureCanReadAsync(
            contractId,
            employeeId,
            cancellationToken);

        var approvalQuery = _dbContext.TblContractApprovalRequests
            .AsNoTracking()
            .Where(candidate => candidate.ContractId == contractId)
            .OrderByDescending(candidate => candidate.SubmittedDate)
            .ThenByDescending(candidate => candidate.ApprovalRequestId);
        var rows = await ApprovalRows(approvalQuery)
            .ToListAsync(cancellationToken);

        return rows
            .OrderByDescending(row => row.Request.SubmittedDate)
            .ThenByDescending(row => row.Request.ApprovalRequestId)
            .Select(MapResponse)
            .ToList();
    }

    public async Task<ContractApprovalActionResponse> DecideAsync(
        int approvalRequestId,
        ApprovalRequestStatus decision,
        ContractApprovalDecisionRequest request,
        int managerEmployeeId,
        CancellationToken cancellationToken = default)
    {
        if (decision is not (
                ApprovalRequestStatus.Approved
                or ApprovalRequestStatus.Returned
                or ApprovalRequestStatus.Rejected))
        {
            throw new ArgumentException("Kết quả duyệt không hợp lệ.");
        }

        ArgumentNullException.ThrowIfNull(request);
        var comment = NormalizeComment(request.Comment);
        if (decision is ApprovalRequestStatus.Returned
                or ApprovalRequestStatus.Rejected
            && comment is null)
        {
            throw Rule(
                StatusCodes.Status400BadRequest,
                ContractApprovalErrorCodes.ApprovalReasonRequired,
                "Return hoặc Reject bắt buộc phải nhập lý do.");
        }

        await EnsureManagerAsync(managerEmployeeId, cancellationToken);
        return await ResolveAsync(
            approvalRequestId,
            decision,
            request.RowVersion,
            comment,
            managerEmployeeId,
            ownerWithdraw: false,
            cancellationToken);
    }

    public async Task<ContractApprovalBulkDecisionResponse> DecideBulkAsync(
        ContractApprovalBulkDecisionRequest request,
        int managerEmployeeId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Items.Count is < 1 or > 100)
        {
            throw new ArgumentException(
                "Mỗi lần chỉ được xử lý từ 1 đến 100 yêu cầu duyệt.");
        }

        if (request.Items
            .GroupBy(item => item.ApprovalRequestId)
            .Any(group => group.Count() > 1))
        {
            throw new ArgumentException(
                "Danh sách bulk không được chứa ApprovalRequestId trùng nhau.");
        }

        if (request.Decision is not (
                ApprovalRequestStatus.Approved
                or ApprovalRequestStatus.Returned
                or ApprovalRequestStatus.Rejected))
        {
            throw new ArgumentException("Kết quả duyệt không hợp lệ.");
        }

        var comment = NormalizeComment(request.Comment);
        if (request.Decision is ApprovalRequestStatus.Returned
                or ApprovalRequestStatus.Rejected
            && comment is null)
        {
            throw Rule(
                StatusCodes.Status400BadRequest,
                ContractApprovalErrorCodes.ApprovalReasonRequired,
                "Return hoặc Reject bắt buộc phải nhập lý do.");
        }

        await EnsureManagerAsync(managerEmployeeId, cancellationToken);

        var itemResults = new List<ContractApprovalBulkDecisionItemResponse>(
            request.Items.Count);
        foreach (var item in request.Items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var result = await ResolveAsync(
                    item.ApprovalRequestId,
                    request.Decision,
                    item.RowVersion,
                    comment,
                    managerEmployeeId,
                    ownerWithdraw: false,
                    cancellationToken);
                itemResults.Add(new ContractApprovalBulkDecisionItemResponse
                {
                    ApprovalRequestId = item.ApprovalRequestId,
                    Success = true,
                    Result = result
                });
            }
            catch (BusinessRuleException exception)
            {
                itemResults.Add(FailedBulkItem(
                    item.ApprovalRequestId,
                    exception.Code,
                    exception.Message));
            }
            catch (RbacOperationException exception)
            {
                itemResults.Add(FailedBulkItem(
                    item.ApprovalRequestId,
                    exception.Code,
                    exception.Message));
            }
            catch (DbUpdateConcurrencyException exception)
            {
                itemResults.Add(FailedBulkItem(
                    item.ApprovalRequestId,
                    AuthorizationErrorCodes.StaleRowVersion,
                    exception.Message));
            }
            catch (KeyNotFoundException exception)
            {
                itemResults.Add(FailedBulkItem(
                    item.ApprovalRequestId,
                    AuthorizationErrorCodes.ResourceNotFound,
                    exception.Message));
            }
            catch (ArgumentException exception)
            {
                itemResults.Add(FailedBulkItem(
                    item.ApprovalRequestId,
                    "InvalidRequest",
                    exception.Message));
            }
        }

        var successCount = itemResults.Count(item => item.Success);
        return new ContractApprovalBulkDecisionResponse
        {
            Decision = request.Decision,
            TotalCount = itemResults.Count,
            SuccessCount = successCount,
            FailureCount = itemResults.Count - successCount,
            Items = itemResults
        };
    }

    public async Task<ContractApprovalActionResponse> WithdrawAsync(
        int approvalRequestId,
        WithdrawContractApprovalRequest request,
        int ownerEmployeeId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var reason = NormalizeComment(request.Reason)
            ?? throw Rule(
                StatusCodes.Status400BadRequest,
                ContractApprovalErrorCodes.ApprovalReasonRequired,
                "Withdraw bắt buộc phải nhập lý do.");

        return await ResolveAsync(
            approvalRequestId,
            ApprovalRequestStatus.Withdrawn,
            request.RowVersion,
            reason,
            ownerEmployeeId,
            ownerWithdraw: true,
            cancellationToken);
    }

    private async Task<ContractApprovalActionResponse> ResolveAsync(
        int approvalRequestId,
        ApprovalRequestStatus decision,
        string rowVersion,
        string? comment,
        int actorEmployeeId,
        bool ownerWithdraw,
        CancellationToken cancellationToken)
    {
        if (approvalRequestId <= 0)
        {
            throw new ArgumentException(
                "ApprovalRequestId phải lớn hơn 0.");
        }

        var expectedRowVersion = DecodeRowVersion(
            rowVersion,
            nameof(rowVersion));
        var strategy = _dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database
                .BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken);
            try
            {
                var approval = await _dbContext.TblContractApprovalRequests
                    .SingleOrDefaultAsync(
                        candidate => candidate.ApprovalRequestId ==
                            approvalRequestId,
                        cancellationToken)
                    ?? throw new KeyNotFoundException(
                        "Không tìm thấy yêu cầu duyệt.");

                EnsureRowVersionMatches(
                    approval.RowVersion,
                    expectedRowVersion,
                    "Yêu cầu duyệt");
                _dbContext.Entry(approval)
                    .Property(candidate => candidate.RowVersion)
                    .OriginalValue = expectedRowVersion;

                if ((ApprovalRequestStatus)approval.Status !=
                    ApprovalRequestStatus.Pending)
                {
                    throw Rule(
                        StatusCodes.Status409Conflict,
                        ContractApprovalErrorCodes
                            .ApprovalRequestAlreadyResolved,
                        "Yêu cầu duyệt đã được xử lý bởi một thao tác khác.");
                }

                var contract = await _dbContext.TblContracts
                    .SingleOrDefaultAsync(
                        candidate => candidate.ContractId ==
                            approval.ContractId,
                        cancellationToken)
                    ?? throw new KeyNotFoundException(
                        "Không tìm thấy hợp đồng của yêu cầu duyệt.");

                var version = await _dbContext.TblContractVersions
                    .SingleOrDefaultAsync(
                        candidate => candidate.VersionId == approval.VersionId
                            && candidate.ContractId == contract.ContractId,
                        cancellationToken)
                    ?? throw new KeyNotFoundException(
                        "Không tìm thấy version đang được duyệt.");

                if ((ContractStatus)contract.Status !=
                        ContractStatus.PendingApproval
                    || contract.CurrentVersionId != version.VersionId)
                {
                    throw Rule(
                        StatusCodes.Status409Conflict,
                        ContractApprovalErrorCodes.ApprovalStateChanged,
                        "Hợp đồng hoặc version đang duyệt đã thay đổi.");
                }

                if (ownerWithdraw)
                {
                    if (contract.EmployeeId != actorEmployeeId
                        || approval.SubmittedByEmployeeId != actorEmployeeId)
                    {
                        throw new RbacOperationException(
                            StatusCodes.Status404NotFound,
                            AuthorizationErrorCodes.ResourceNotFound,
                            "Resource was not found.");
                    }
                }
                else
                {
                    if (approval.SubmittedByEmployeeId == actorEmployeeId)
                    {
                        throw Rule(
                            StatusCodes.Status403Forbidden,
                            ContractApprovalErrorCodes.SelfApprovalDenied,
                            "Người gửi duyệt không được tự xử lý yêu cầu của mình.");
                    }

                    await EnsureWorkflowAllowsAsync(
                        approval,
                        actorEmployeeId,
                        cancellationToken);
                }

                ApprovalRequestPolicy.EnsureCanApplyResult(
                    (ApprovalRequestStatus)approval.Status,
                    decision,
                    (ContractStatus)contract.Status);

                if (decision == ApprovalRequestStatus.Approved)
                {
                    if (!version.IsLocked
                        || string.IsNullOrWhiteSpace(version.SnapshotJson)
                        || string.IsNullOrWhiteSpace(version.SnapshotHash))
                    {
                        throw ArtifactMissing();
                    }

                    await VerifySubmittedArtifactsAsync(
                        version.VersionId,
                        cancellationToken);
                }

                var previousContractStatus = contract.Status;
                var now = DateTime.UtcNow;
                var targetContractStatus =
                    ApprovalRequestPolicy.GetTargetContractStatus(decision);

                approval.Status = (byte)decision;
                approval.ResolvedByEmployeeId = actorEmployeeId;
                approval.ResolvedDate = now;
                approval.DecisionComment = comment;

                contract.Status = (byte)targetContractStatus;
                contract.UpdatedEmployeeId = actorEmployeeId;
                contract.UpdateDate = now;

                _dbContext.TblApprovalHistories.Add(new TblApprovalHistory
                {
                    WorkflowId = approval.WorkflowId,
                    ObjectType = ApprovalHistoryObjectType,
                    ObjectId = approval.ApprovalRequestId,
                    StepNo = 1,
                    ApproverEmployeeId = actorEmployeeId,
                    ApprovalAction = decision.ToString(),
                    Comment = comment,
                    ActionDate = now
                });

                _auditWriter.StageEmployeeAudits(
                [
                    new EmployeeContractAuditWriteRequest(
                        contract.ContractId,
                        version.VersionId,
                        actorEmployeeId,
                        AuditAction(decision),
                        ContractAuditResults.Succeeded,
                        now,
                        PreviousContractStatus: previousContractStatus,
                        NewContractStatus: contract.Status,
                        Reason: comment,
                        SubjectType:
                            ContractAuditSubjectTypes.ApprovalRequest,
                        SubjectId: approval.ApprovalRequestId,
                        PreviousValues: ContractAuditValues.Create(
                            ("Status", previousContractStatus),
                            ("CurrentVersionId", version.VersionId),
                            ("ApprovalRequestId",
                                approval.ApprovalRequestId),
                            ("ApprovalStatus",
                                (byte)ApprovalRequestStatus.Pending),
                            ("VersionLocked", version.IsLocked)),
                        NewValues: ContractAuditValues.Create(
                            ("Status", contract.Status),
                            ("CurrentVersionId", version.VersionId),
                            ("ApprovalRequestId",
                                approval.ApprovalRequestId),
                            ("ApprovalStatus", approval.Status),
                            ("ResolvedByEmployeeId", actorEmployeeId),
                            ("VersionLocked", version.IsLocked)))
                ]);

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return new ContractApprovalActionResponse
                {
                    ApprovalRequestId = approval.ApprovalRequestId,
                    ContractId = contract.ContractId,
                    VersionId = version.VersionId,
                    ApprovalStatus = decision,
                    ContractStatus = targetContractStatus,
                    ResolvedByEmployeeId = actorEmployeeId,
                    ResolvedDate = now,
                    DecisionComment = comment,
                    ApprovalRequestRowVersion =
                        EncodeRowVersion(approval.RowVersion),
                    ContractRowVersion = EncodeRowVersion(contract.RowVersion)
                };
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                _dbContext.ChangeTracker.Clear();
                throw;
            }
        });
    }

    private IQueryable<ApprovalRow> ApprovalRows(
        IQueryable<TblContractApprovalRequest> approvalRequests)
    {
        return
            from request in approvalRequests
            join contract in _dbContext.TblContracts.AsNoTracking()
                on request.ContractId equals contract.ContractId
            join version in _dbContext.TblContractVersions.AsNoTracking()
                on request.VersionId equals version.VersionId
            join submitter in _dbContext.TblEmployees.AsNoTracking()
                on request.SubmittedByEmployeeId equals submitter.EmployeeId
            join owner in _dbContext.TblEmployees.AsNoTracking()
                on contract.EmployeeId equals owner.EmployeeId
            join resolverCandidate in _dbContext.TblEmployees.AsNoTracking()
                on request.ResolvedByEmployeeId equals
                    (int?)resolverCandidate.EmployeeId into resolverRows
            from resolver in resolverRows.DefaultIfEmpty()
            select new ApprovalRow(
                request,
                contract,
                version,
                submitter,
                owner,
                resolver);
    }

    private async Task EnsureManagerAsync(
        int employeeId,
        CancellationToken cancellationToken)
    {
        var manager = await _dbContext.TblEmployees
            .AsNoTracking()
            .AnyAsync(employee =>
                employee.EmployeeId == employeeId
                && employee.Status == ActiveEmployeeStatus
                && employee.EmployeeType == (byte)EmployeeType.Manager,
                cancellationToken);

        if (!manager)
        {
            throw new RbacOperationException(
                StatusCodes.Status403Forbidden,
                AuthorizationErrorCodes.PermissionDenied,
                "Chỉ Manager active được xử lý yêu cầu duyệt.");
        }
    }

    private async Task EnsureWorkflowAllowsAsync(
        TblContractApprovalRequest approval,
        int managerEmployeeId,
        CancellationToken cancellationToken)
    {
        if (!approval.WorkflowId.HasValue)
        {
            return;
        }

        var workflow = await _dbContext.TblApprovalWorkflows
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate =>
                candidate.WorkflowId == approval.WorkflowId.Value
                && candidate.ObjectType == "Contract"
                && candidate.StepNo == 1
                && candidate.IsActive,
                cancellationToken)
            ?? throw new RbacOperationException(
                StatusCodes.Status403Forbidden,
                AuthorizationErrorCodes.PermissionDenied,
                "Workflow duyệt không còn khả dụng.");

        if (workflow.ApproverEmployeeId.HasValue
            && workflow.ApproverEmployeeId.Value != managerEmployeeId)
        {
            throw new RbacOperationException(
                StatusCodes.Status403Forbidden,
                AuthorizationErrorCodes.PermissionDenied,
                "Yêu cầu duyệt được giao cho Manager khác.");
        }
    }

    private async Task<IReadOnlyList<ContractApprovalArtifactResponse>>
        LoadArtifactsAsync(
            int versionId,
            CancellationToken cancellationToken)
    {
        return await _dbContext.TblFileStorages
            .AsNoTracking()
            .Where(file =>
                file.ObjectType == SubmittedArtifactObjectType
                && file.ObjectId == versionId)
            .OrderBy(file => file.FileType)
            .Select(file => new ContractApprovalArtifactResponse
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

    private async Task VerifySubmittedArtifactsAsync(
        int versionId,
        CancellationToken cancellationToken)
    {
        var artifacts = await _dbContext.TblFileStorages
            .AsNoTracking()
            .Where(file =>
                file.ObjectType == SubmittedArtifactObjectType
                && file.ObjectId == versionId)
            .ToListAsync(cancellationToken);

        var expectedTypes = new[] { "docx", "pdf" };
        if (artifacts.Count != expectedTypes.Length
            || expectedTypes.Any(type =>
                artifacts.Count(file => string.Equals(
                    file.FileType,
                    type,
                    StringComparison.OrdinalIgnoreCase)) != 1))
        {
            throw ArtifactMissing();
        }

        foreach (var artifact in artifacts)
        {
            if (string.IsNullOrWhiteSpace(artifact.StorageKey)
                || string.IsNullOrWhiteSpace(artifact.TenantCode)
                || string.IsNullOrWhiteSpace(artifact.Sha256))
            {
                throw ArtifactMissing();
            }

            try
            {
                await using var stream = await _privateFileStorage.OpenReadAsync(
                    artifact.TenantCode,
                    artifact.StorageKey,
                    cancellationToken);
                var actualHash = Convert.ToHexString(
                        await SHA256.HashDataAsync(stream, cancellationToken))
                    .ToLowerInvariant();
                if (!string.Equals(
                        actualHash,
                        artifact.Sha256,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw ArtifactMissing();
                }
            }
            catch (BusinessRuleException)
            {
                throw;
            }
            catch (Exception exception) when (
                exception is FileNotFoundException
                    or UnauthorizedAccessException
                    or ArgumentException)
            {
                throw ArtifactMissing();
            }
        }
    }

    private static ContractApprovalRequestResponse MapResponse(
        ApprovalRow row) => new()
        {
            ApprovalRequestId = row.Request.ApprovalRequestId,
            ContractId = row.Contract.ContractId,
            ContractCode = row.Contract.ContractCode,
            ContractName = row.Contract.ContractName,
            ResponsibleEmployeeId = row.Contract.EmployeeId,
            ResponsibleEmployeeName = row.Owner.EmployeeFullName,
            VersionId = row.Version.VersionId,
            VersionNo = row.Version.VersionNo,
            SnapshotHash = row.Version.SnapshotHash,
            Status = (ApprovalRequestStatus)row.Request.Status,
            SubmittedByEmployeeId = row.Request.SubmittedByEmployeeId,
            SubmittedByEmployeeName = row.Submitter.EmployeeFullName,
            SubmittedDate = row.Request.SubmittedDate,
            ResolvedByEmployeeId = row.Request.ResolvedByEmployeeId,
            ResolvedByEmployeeName = row.Resolver?.EmployeeFullName,
            ResolvedDate = row.Request.ResolvedDate,
            DecisionComment = row.Request.DecisionComment,
            RowVersion = EncodeRowVersion(row.Request.RowVersion)
        };

    private static ContractApprovalDetailResponse MapDetail(
        ApprovalRow row)
    {
        var source = MapResponse(row);
        return new ContractApprovalDetailResponse
        {
            ApprovalRequestId = source.ApprovalRequestId,
            ContractId = source.ContractId,
            ContractCode = source.ContractCode,
            ContractName = source.ContractName,
            ResponsibleEmployeeId = source.ResponsibleEmployeeId,
            ResponsibleEmployeeName = source.ResponsibleEmployeeName,
            VersionId = source.VersionId,
            VersionNo = source.VersionNo,
            SnapshotHash = source.SnapshotHash,
            Status = source.Status,
            SubmittedByEmployeeId = source.SubmittedByEmployeeId,
            SubmittedByEmployeeName = source.SubmittedByEmployeeName,
            SubmittedDate = source.SubmittedDate,
            ResolvedByEmployeeId = source.ResolvedByEmployeeId,
            ResolvedByEmployeeName = source.ResolvedByEmployeeName,
            ResolvedDate = source.ResolvedDate,
            DecisionComment = source.DecisionComment,
            RowVersion = source.RowVersion
        };
    }

    private static string AuditAction(ApprovalRequestStatus decision) =>
        decision switch
        {
            ApprovalRequestStatus.Approved =>
                ContractAuditActionTypes.ApprovalApproved,
            ApprovalRequestStatus.Returned =>
                ContractAuditActionTypes.ApprovalReturned,
            ApprovalRequestStatus.Rejected =>
                ContractAuditActionTypes.ApprovalRejected,
            ApprovalRequestStatus.Withdrawn =>
                ContractAuditActionTypes.ApprovalWithdrawn,
            _ => throw new ArgumentOutOfRangeException(nameof(decision))
        };

    private static string? NormalizeComment(string? value)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        return normalized.Length <= 1000
            ? normalized
            : throw new ArgumentException(
                "Nội dung quyết định không vượt quá 1000 ký tự.");
    }

    private static byte[] DecodeRowVersion(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                $"{fieldName} không được để trống.");
        }

        try
        {
            var bytes = Convert.FromBase64String(value);
            return bytes.Length > 0
                ? bytes
                : throw new FormatException();
        }
        catch (FormatException)
        {
            throw new ArgumentException(
                $"{fieldName} không đúng định dạng Base64.");
        }
    }

    private static string EncodeRowVersion(byte[]? value) =>
        Convert.ToBase64String(value ?? Array.Empty<byte>());

    private static void EnsureRowVersionMatches(
        byte[]? current,
        byte[] expected,
        string resourceName)
    {
        if (current is null
            || !current.AsSpan().SequenceEqual(expected))
        {
            throw new DbUpdateConcurrencyException(
                $"{resourceName} đã được cập nhật bởi request khác.");
        }
    }

    private static BusinessRuleException ArtifactMissing() => Rule(
        StatusCodes.Status409Conflict,
        ContractApprovalErrorCodes.ApprovalArtifactMissing,
        "DOCX/PDF bất biến của version gửi duyệt bị thiếu hoặc sai hash.");

    private static ContractApprovalBulkDecisionItemResponse FailedBulkItem(
        int approvalRequestId,
        string errorCode,
        string errorMessage) => new()
        {
            ApprovalRequestId = approvalRequestId,
            Success = false,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage
        };

    private static BusinessRuleException Rule(
        int statusCode,
        string code,
        string message) => new(statusCode, code, message);

    private sealed record ApprovalRow(
        TblContractApprovalRequest Request,
        TblContract Contract,
        TblContractVersion Version,
        TblEmployee Submitter,
        TblEmployee Owner,
        TblEmployee? Resolver);
}
