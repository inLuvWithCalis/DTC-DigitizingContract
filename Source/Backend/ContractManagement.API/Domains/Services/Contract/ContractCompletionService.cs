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

public sealed class ContractCompletionService : IContractCompletionService
{
    private const string AcceptanceObjectType = "ContractAcceptanceEvidence";
    private const string PaymentObjectType = "ContractPaymentEvidence";
    private readonly DbDtctechContext _db;
    private readonly IContractResourceAuthorizationService _authorization;
    private readonly IContractAuditWriter _audit;
    private readonly IPrivateFileStorage _files;
    private readonly ICurrentTenant _tenant;
    private readonly IContractPaymentDueDateService _paymentDueDates;

    public ContractCompletionService(DbDtctechContext db,
        IContractResourceAuthorizationService authorization,
        IContractAuditWriter audit, IPrivateFileStorage files,
        ICurrentTenant tenant, IContractPaymentDueDateService? paymentDueDates = null)
    {
        _db = db;
        _authorization = authorization;
        _audit = audit;
        _files = files;
        _tenant = tenant;
        _paymentDueDates = paymentDueDates ?? new ContractPaymentDueDateService();
    }

    public async Task<ContractCompletionDetailResponse> GetAsync(int contractId,
        int employeeId, CancellationToken cancellationToken = default)
    {
        await _authorization.EnsureCanReadAsync(contractId, employeeId, cancellationToken);
        return await LoadDetailAsync(contractId, cancellationToken);
    }

    public async Task<ContractCompletionReadinessResponse> GetReadinessAsync(
        int contractId, int employeeId, CancellationToken cancellationToken = default) =>
        (await GetAsync(contractId, employeeId, cancellationToken)).Readiness;

    public async Task<IReadOnlyList<ContractPaymentMilestoneResponse>>
        GetPaymentMilestonesAsync(int contractId, int versionId, int employeeId,
            CancellationToken cancellationToken = default)
    {
        var detail = await GetAsync(contractId, employeeId, cancellationToken);
        if (detail.VersionId != versionId)
            throw new KeyNotFoundException("Không tìm thấy version hợp đồng hiện hành.");
        return detail.PaymentMilestones;
    }

    public async Task<ContractAcceptanceEvidenceResponse> UploadAcceptanceAsync(
        int contractId, UploadContractAcceptanceEvidenceRequest request,
        int employeeId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.File is null || request.File.Length <= 0) throw Rule("AcceptanceFileRequired", "Vui lòng chọn biên bản nghiệm thu.");
        await _authorization.EnsureCanManageAcceptanceAsync(
            contractId,
            employeeId,
            cancellationToken);
        var stored = await SaveFileAsync(request.File, AcceptanceObjectType, contractId, cancellationToken);
        try
        {
            return await ExecuteInTransactionAsync(async () =>
            {
                var (contract, version) = await LoadWritableStateAsync(contractId, request.CurrentVersionId,
                    request.ContractRowVersion, request.VersionRowVersion, cancellationToken);
                EnsureSigned(contract, version);
                if (await _db.TblContractAcceptanceEvidences.AnyAsync(x => x.ContractId == contractId && x.VersionId == version.VersionId, cancellationToken))
                    throw Rule("AcceptanceEvidenceExists", "Version này đã có biên bản nghiệm thu.", StatusCodes.Status409Conflict);

                var metadata = CreateFile(stored, AcceptanceObjectType, contractId, employeeId);
                _db.TblFileStorages.Add(metadata);
                await _db.SaveChangesAsync(cancellationToken);
                var evidence = new TblContractAcceptanceEvidence
                {
                    ContractId = contractId, VersionId = version.VersionId, FileId = metadata.FileId,
                    UploadedByEmployeeId = employeeId, UploadedAt = DateTime.UtcNow
                };
                _db.TblContractAcceptanceEvidences.Add(evidence);
                await _db.SaveChangesAsync(cancellationToken);
                await ActivateMilestonesAsync(version.VersionId,
                    PaymentDueAnchor.AcceptanceCompleted, evidence.UploadedAt.Date,
                    employeeId, cancellationToken);
                _audit.StageEmployeeAudits([new(contractId, version.VersionId, employeeId,
                    ContractAuditActionTypes.AcceptanceEvidenceUploaded, ContractAuditResults.Succeeded,
                    evidence.UploadedAt, SubjectType: ContractAuditSubjectTypes.AcceptanceEvidence,
                    SubjectId: evidence.AcceptanceEvidenceId,
                    NewValues: ContractAuditValues.Create(("AcceptanceEvidenceId", evidence.AcceptanceEvidenceId),
                        ("FileId", metadata.FileId), ("FileType", metadata.FileType),
                        ("Sha256", metadata.Sha256), ("CurrentVersionId", version.VersionId))) ]);
                await _db.SaveChangesAsync(cancellationToken);
                return await LoadAcceptanceAsync(evidence.AcceptanceEvidenceId, cancellationToken);
            }, cancellationToken);
        }
        catch
        {
            _db.ChangeTracker.Clear();
            await _files.DeleteAsync(stored.TenantCode, stored.StorageKey, cancellationToken);
            throw;
        }
    }

    public async Task<ContractPaymentResponse> AddPaymentAsync(int contractId,
        AddContractPaymentRequest request, int employeeId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await _authorization.EnsureCanManagePaymentAsync(
            contractId,
            employeeId,
            cancellationToken);
        if (request.Amount <= 0) throw Rule("PaymentAmountInvalid", "Số tiền thanh toán phải lớn hơn 0.");
        if (request.PaymentDate == default || request.PaymentDate.Date > DateTime.UtcNow.Date)
            throw Rule("PaymentDateInvalid", "Ngày thanh toán không hợp lệ hoặc nằm trong tương lai.");
        var method = Required(request.PaymentMethod, 100, "Phương thức thanh toán");
        var reference = Required(request.ReferenceCode, 100, "Mã tham chiếu").ToUpperInvariant();
        var currency = Required(request.CurrencyCode, 3, "Loại tiền").ToUpperInvariant();
        StoredPrivateFile? stored = request.EvidenceFile is null ? null
            : await SaveFileAsync(request.EvidenceFile, PaymentObjectType, contractId, cancellationToken);
        try
        {
            return await ExecuteInTransactionAsync(async () =>
            {
                var (contract, version) = await LoadWritableStateAsync(contractId, request.CurrentVersionId,
                    request.ContractRowVersion, request.VersionRowVersion, cancellationToken);
                EnsureSigned(contract, version);
                if (!string.Equals(currency, version.CurrencyCode, StringComparison.OrdinalIgnoreCase))
                    throw Rule("PaymentCurrencyMismatch", "Loại tiền thanh toán phải trùng với loại tiền của hợp đồng.");
                if (await _db.TblContractPaymentLedgers.AnyAsync(x => x.VersionId == version.VersionId && x.ReferenceCode == reference, cancellationToken))
                    throw Rule("PaymentReferenceDuplicated", "Mã tham chiếu đã tồn tại trong version hợp đồng.", StatusCodes.Status409Conflict);
                var hasMilestones = await _db.TblContractPaymentMilestones.AnyAsync(
                    x => x.VersionId == version.VersionId, cancellationToken);
                if (hasMilestones)
                    throw Rule("StructuredPaymentUsesMilestoneStatus",
                        "Hợp đồng này theo dõi thanh toán bằng trạng thái từng đợt.",
                        StatusCodes.Status409Conflict);
                var paid = await _db.TblContractPaymentLedgers.Where(x => x.VersionId == version.VersionId && x.Status == (byte)ContractPaymentStatus.Active).SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0;
                if (paid + request.Amount > version.TotalAmount)
                    throw Rule("PaymentExceedsContractTotal", "Tổng thanh toán không được vượt quá giá trị hợp đồng.", StatusCodes.Status409Conflict);

                int? fileId = null;
                if (stored is not null)
                {
                    var metadata = CreateFile(stored, PaymentObjectType, contractId, employeeId);
                    _db.TblFileStorages.Add(metadata);
                    await _db.SaveChangesAsync(cancellationToken);
                    fileId = metadata.FileId;
                }
                var payment = new TblContractPaymentLedger
                {
                    ContractId = contractId, VersionId = version.VersionId,
                    PaymentMilestoneId = null,
                    PaymentDate = request.PaymentDate.Date, Amount = request.Amount,
                    CurrencyCode = currency, PaymentMethod = method, ReferenceCode = reference,
                    EvidenceFileId = fileId, Status = (byte)ContractPaymentStatus.Active,
                    CreatedByEmployeeId = employeeId, CreatedAt = DateTime.UtcNow
                };
                _db.TblContractPaymentLedgers.Add(payment);
                await _db.SaveChangesAsync(cancellationToken);
                _audit.StageEmployeeAudits([new(contractId, version.VersionId, employeeId,
                    ContractAuditActionTypes.PaymentAdded, ContractAuditResults.Succeeded, payment.CreatedAt,
                    SubjectType: ContractAuditSubjectTypes.Payment, SubjectId: payment.ContractPaymentId,
                    NewValues: PaymentAudit(payment, paid + payment.Amount, version.TotalAmount - paid - payment.Amount))]);
                await _db.SaveChangesAsync(cancellationToken);
                return await LoadPaymentAsync(payment.ContractPaymentId, cancellationToken);
            }, cancellationToken);
        }
        catch
        {
            _db.ChangeTracker.Clear();
            if (stored is not null) await _files.DeleteAsync(stored.TenantCode, stored.StorageKey, cancellationToken);
            throw;
        }
    }

    public async Task<ContractPaymentMilestoneResponse> SetPaymentMilestoneStatusAsync(
        int contractId, int versionId, int milestoneId,
        SetContractPaymentMilestoneStatusRequest request, int employeeId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await _authorization.EnsureCanManagePaymentAsync(
            contractId, employeeId, cancellationToken);
        if (request.CurrentVersionId != versionId)
            throw Rule("CompletionStateChanged", "Version hiện hành đã thay đổi.",
                StatusCodes.Status409Conflict);
        if (!Enum.IsDefined(request.Status))
            throw Rule("PaymentMilestoneStatusInvalid",
                "Trạng thái đợt thanh toán không hợp lệ.");

        return await ExecuteInTransactionAsync(async () =>
        {
            var (contract, version) = await LoadWritableStateAsync(contractId,
                request.CurrentVersionId, request.ContractRowVersion,
                request.VersionRowVersion, cancellationToken);
            EnsureSigned(contract, version);
            var milestone = await _db.TblContractPaymentMilestones.SingleOrDefaultAsync(
                x => x.PaymentMilestoneId == milestoneId
                    && x.ContractId == contractId && x.VersionId == versionId,
                cancellationToken)
                ?? throw new KeyNotFoundException("Không tìm thấy đợt thanh toán.");
            Match(milestone.RowVersion, Decode(request.MilestoneRowVersion),
                "Đợt thanh toán");

            var currentStatus = (ContractPaymentMilestoneStatus)milestone.PaymentStatus;
            if (currentStatus == request.Status)
                return (await LoadDetailAsync(contractId, cancellationToken))
                    .PaymentMilestones.Single(x => x.PaymentMilestoneId == milestoneId);

            var now = DateTime.UtcNow;
            var previousValues = ContractAuditValues.Create(
                ("PaymentMilestoneId", milestone.PaymentMilestoneId),
                ("CurrentVersionId", version.VersionId),
                ("PaymentStatus", milestone.PaymentStatus),
                ("PaidAt", milestone.PaidAt),
                ("PaidByEmployeeId", milestone.PaidByEmployeeId));

            if (request.Status == ContractPaymentMilestoneStatus.Paid)
            {
                milestone.PaymentStatus = (byte)ContractPaymentMilestoneStatus.Paid;
                milestone.PaidAt = now;
                milestone.PaidByEmployeeId = employeeId;

                var next = await _db.TblContractPaymentMilestones
                    .Where(x => x.VersionId == versionId
                        && x.DisplayOrder > milestone.DisplayOrder)
                    .OrderBy(x => x.DisplayOrder)
                    .ThenBy(x => x.PaymentMilestoneId)
                    .FirstOrDefaultAsync(cancellationToken);
                if (next?.DueAnchor == (byte)PaymentDueAnchor.PreviousMilestonePaid
                    && !next.AnchorDate.HasValue)
                {
                    next.AnchorDate = now.Date;
                    next.DueDate = _paymentDueDates.Calculate(now.Date,
                        next.DueOffsetDays,
                        (PaymentDayCountMode)next.DayCountMode);
                    next.UpdatedEmployeeId = employeeId;
                    next.UpdatedDate = now;
                }
            }
            else
            {
                var hasPaidDownstream = await _db.TblContractPaymentMilestones.AnyAsync(
                    x => x.VersionId == versionId
                        && x.DisplayOrder > milestone.DisplayOrder
                        && x.PaymentStatus == (byte)ContractPaymentMilestoneStatus.Paid,
                    cancellationToken);
                if (hasPaidDownstream)
                    throw Rule("PaymentMilestoneDownstreamPaid",
                        "Không thể chuyển về chưa nộp khi một đợt phía sau đã được đánh dấu đã nộp.",
                        StatusCodes.Status409Conflict);

                milestone.PaymentStatus = (byte)ContractPaymentMilestoneStatus.Unpaid;
                milestone.PaidAt = null;
                milestone.PaidByEmployeeId = null;
                var next = await _db.TblContractPaymentMilestones
                    .Where(x => x.VersionId == versionId
                        && x.DisplayOrder > milestone.DisplayOrder)
                    .OrderBy(x => x.DisplayOrder)
                    .ThenBy(x => x.PaymentMilestoneId)
                    .FirstOrDefaultAsync(cancellationToken);
                if (next?.DueAnchor == (byte)PaymentDueAnchor.PreviousMilestonePaid
                    && next.PaymentStatus == (byte)ContractPaymentMilestoneStatus.Unpaid)
                {
                    next.AnchorDate = null;
                    next.DueDate = null;
                    next.UpdatedEmployeeId = employeeId;
                    next.UpdatedDate = now;
                }
            }

            milestone.UpdatedEmployeeId = employeeId;
            milestone.UpdatedDate = now;
            _audit.StageEmployeeAudits([new(contractId, version.VersionId, employeeId,
                ContractAuditActionTypes.PaymentMilestoneStatusChanged,
                ContractAuditResults.Succeeded, now,
                SubjectType: ContractAuditSubjectTypes.Payment,
                SubjectId: milestone.PaymentMilestoneId,
                PreviousValues: previousValues,
                NewValues: ContractAuditValues.Create(
                    ("PaymentMilestoneId", milestone.PaymentMilestoneId),
                    ("CurrentVersionId", version.VersionId),
                    ("PaymentStatus", milestone.PaymentStatus),
                    ("PaidAt", milestone.PaidAt),
                    ("PaidByEmployeeId", milestone.PaidByEmployeeId))) ]);
            await _db.SaveChangesAsync(cancellationToken);
            return (await LoadDetailAsync(contractId, cancellationToken))
                .PaymentMilestones.Single(x => x.PaymentMilestoneId == milestoneId);
        }, cancellationToken);
    }

    public async Task<ContractPaymentResponse> VoidPaymentAsync(int contractId,
        int paymentId, VoidContractPaymentRequest request, int employeeId,
        CancellationToken cancellationToken = default)
    {
        await _authorization.EnsureCanManagePaymentAsync(
            contractId,
            employeeId,
            cancellationToken);
        var reason = Required(request.Reason, 1000, "Lý do hủy khoản thanh toán");
        return await ExecuteInTransactionAsync(async () =>
        {
            var payment = await _db.TblContractPaymentLedgers.SingleOrDefaultAsync(x => x.ContractPaymentId == paymentId && x.ContractId == contractId, cancellationToken)
                ?? throw new KeyNotFoundException("Không tìm thấy khoản thanh toán.");
            var (contract, version) = await LoadWritableStateAsync(contractId, payment.VersionId,
                request.ContractRowVersion, request.VersionRowVersion, cancellationToken);
            EnsureSigned(contract, version);
            if (await _db.TblContractPaymentMilestones.AnyAsync(
                    x => x.VersionId == version.VersionId, cancellationToken))
                throw Rule("StructuredPaymentUsesMilestoneStatus",
                    "Hợp đồng này theo dõi thanh toán bằng trạng thái từng đợt.",
                    StatusCodes.Status409Conflict);
            Match(payment.RowVersion, Decode(request.PaymentRowVersion), "Khoản thanh toán");
            if (payment.Status != (byte)ContractPaymentStatus.Active)
                throw Rule("PaymentAlreadyVoided", "Khoản thanh toán đã bị hủy.", StatusCodes.Status409Conflict);
            var paidBefore = await _db.TblContractPaymentLedgers.Where(x => x.VersionId == version.VersionId && x.Status == (byte)ContractPaymentStatus.Active).SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0;
            var previousValues = PaymentAudit(payment, paidBefore, version.TotalAmount - paidBefore);
            payment.Status = (byte)ContractPaymentStatus.Voided;
            payment.VoidReason = reason; payment.VoidedByEmployeeId = employeeId; payment.VoidedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            _audit.StageEmployeeAudits([new(contractId, version.VersionId, employeeId,
                ContractAuditActionTypes.PaymentVoided, ContractAuditResults.Succeeded, payment.VoidedAt.Value,
                Reason: reason, SubjectType: ContractAuditSubjectTypes.Payment, SubjectId: payment.ContractPaymentId,
                PreviousValues: previousValues,
                NewValues: PaymentAudit(payment, paidBefore - payment.Amount, version.TotalAmount - paidBefore + payment.Amount))]);
            await _db.SaveChangesAsync(cancellationToken);
            return await LoadPaymentAsync(paymentId, cancellationToken);
        }, cancellationToken);
    }

    public async Task<ContractCompletionDetailResponse> CompleteAsync(int contractId,
        CompleteContractRequest request, int employeeId,
        CancellationToken cancellationToken = default)
    {
        await _authorization.EnsureCanCompleteAsync(
            contractId,
            employeeId,
            cancellationToken);
        return await ExecuteInTransactionAsync(async () =>
        {
            var contract = await _db.TblContracts.SingleOrDefaultAsync(x => x.ContractId == contractId, cancellationToken)
                ?? throw new KeyNotFoundException("Không tìm thấy hợp đồng.");
            Match(contract.RowVersion, Decode(request.ContractRowVersion), "Hợp đồng");
            if (contract.CurrentVersionId != request.CurrentVersionId) throw Rule("CompletionStateChanged", "Version hiện hành đã thay đổi.", StatusCodes.Status409Conflict);
            var version = await _db.TblContractVersions.SingleAsync(x => x.VersionId == request.CurrentVersionId && x.ContractId == contractId, cancellationToken);
            Match(version.RowVersion, Decode(request.VersionRowVersion), "Version hợp đồng");
            var readiness = await BuildReadinessAsync(contract, version, cancellationToken);
            if (!readiness.Ready) throw Rule("ContractNotReadyForCompletion", "Hợp đồng chưa đủ điều kiện hoàn tất.", StatusCodes.Status409Conflict);
            var previous = contract.Status;
            contract.Status = (byte)ContractStatus.Completed; contract.UpdatedEmployeeId = employeeId; contract.UpdateDate = DateTime.UtcNow;
            _audit.StageEmployeeAudits([new(contractId, version.VersionId, employeeId,
                ContractAuditActionTypes.ContractCompleted, ContractAuditResults.Succeeded, contract.UpdateDate.Value,
                PreviousContractStatus: previous, NewContractStatus: contract.Status,
                NewValues: ContractAuditValues.Create(("Status", contract.Status), ("CurrentVersionId", version.VersionId),
                    ("TotalAmount", readiness.TotalAmount), ("PaidAmount", readiness.PaidAmount))) ]);
            await _db.SaveChangesAsync(cancellationToken);
            return await LoadDetailAsync(contractId, cancellationToken);
        }, cancellationToken);
    }

    private async Task<ContractCompletionDetailResponse> LoadDetailAsync(int contractId, CancellationToken ct)
    {
        var contract = await _db.TblContracts.AsNoTracking().SingleOrDefaultAsync(x => x.ContractId == contractId, ct) ?? throw new KeyNotFoundException("Không tìm thấy hợp đồng.");
        if (!contract.CurrentVersionId.HasValue) throw new InvalidOperationException("Hợp đồng chưa có version hiện hành.");
        var version = await _db.TblContractVersions.AsNoTracking().SingleAsync(x => x.VersionId == contract.CurrentVersionId && x.ContractId == contractId, ct);
        var acceptanceId = await _db.TblContractAcceptanceEvidences.AsNoTracking().Where(x => x.ContractId == contractId && x.VersionId == version.VersionId).Select(x => (int?)x.AcceptanceEvidenceId).SingleOrDefaultAsync(ct);
        var paymentIds = await _db.TblContractPaymentLedgers.AsNoTracking().Where(x => x.ContractId == contractId && x.VersionId == version.VersionId).OrderByDescending(x => x.PaymentDate).ThenByDescending(x => x.ContractPaymentId).Select(x => x.ContractPaymentId).ToListAsync(ct);
        var payments = new List<ContractPaymentResponse>();
        foreach (var id in paymentIds) payments.Add(await LoadPaymentAsync(id, ct));
        var milestoneRows = await _db.TblContractPaymentMilestones.AsNoTracking()
            .Where(x => x.ContractId == contractId && x.VersionId == version.VersionId)
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.PaymentMilestoneId).ToListAsync(ct);
        var milestoneResponses = milestoneRows.Select(row =>
        {
            var paymentStatus = (ContractPaymentMilestoneStatus)row.PaymentStatus;
            var paidAmount = paymentStatus == ContractPaymentMilestoneStatus.Paid
                ? row.Amount : 0m;
            var isOverdue = paymentStatus == ContractPaymentMilestoneStatus.Unpaid
                && row.DueDate.HasValue
                && row.DueDate.Value.Date < DateTime.UtcNow.Date;
            return new ContractPaymentMilestoneResponse
            {
                PaymentMilestoneId = row.PaymentMilestoneId,
                SourceTemplatePaymentMilestoneId = row.SourceTemplatePaymentMilestoneId,
                VersionId = row.VersionId,
                MilestoneCode = row.MilestoneCode, TitleVi = row.TitleVi,
                TitleEn = row.TitleEn, PaymentPercent = row.PaymentPercent,
                Amount = row.Amount, PaidAmount = paidAmount,
                RemainingAmount = row.Amount - paidAmount,
                DueAnchor = (PaymentDueAnchor)row.DueAnchor,
                DueOffsetDays = row.DueOffsetDays,
                DayCountMode = (PaymentDayCountMode)row.DayCountMode,
                ConditionVi = row.ConditionVi, ConditionEn = row.ConditionEn,
                DisplayOrder = row.DisplayOrder, AnchorDate = row.AnchorDate,
                DueDate = row.DueDate, PaymentStatus = paymentStatus,
                IsOverdue = isOverdue, PaidAt = row.PaidAt,
                PaidByEmployeeId = row.PaidByEmployeeId,
                RowVersion = Encode(row.RowVersion)
            };
        }).ToList();
        return new ContractCompletionDetailResponse { ContractId = contractId, ContractStatus = (ContractStatus)contract.Status,
            VersionId = version.VersionId, VersionNo = version.VersionNo, ContractRowVersion = Encode(contract.RowVersion), VersionRowVersion = Encode(version.RowVersion),
            AcceptanceEvidence = acceptanceId.HasValue ? await LoadAcceptanceAsync(acceptanceId.Value, ct) : null,
            Payments = milestoneRows.Count == 0 ? payments : [],
            PaymentMilestones = milestoneResponses,
            Readiness = await BuildReadinessAsync(contract, version, ct) };
    }

    private async Task<ContractCompletionReadinessResponse> BuildReadinessAsync(TblContract contract, TblContractVersion version, CancellationToken ct)
    {
        var signed = await _db.TblContractSignedEvidences.AsNoTracking().AnyAsync(x => x.ContractId == contract.ContractId && x.VersionId == version.VersionId && x.Status == (byte)SignedEvidenceStatus.Active, ct);
        var acceptance = await _db.TblContractAcceptanceEvidences.AsNoTracking().AnyAsync(x => x.ContractId == contract.ContractId && x.VersionId == version.VersionId, ct);
        var milestoneAmounts = await _db.TblContractPaymentMilestones.AsNoTracking()
            .Where(x => x.VersionId == version.VersionId)
            .Select(x => new { x.Amount, x.PaymentStatus })
            .ToListAsync(ct);
        var paid = milestoneAmounts.Count > 0
            ? milestoneAmounts
                .Where(x => x.PaymentStatus == (byte)ContractPaymentMilestoneStatus.Paid)
                .Sum(x => x.Amount)
            : await _db.TblContractPaymentLedgers.AsNoTracking()
                .Where(x => x.VersionId == version.VersionId
                    && x.Status == (byte)ContractPaymentStatus.Active)
                .SumAsync(x => (decimal?)x.Amount, ct) ?? 0;
        var allStructuredMilestonesPaid = milestoneAmounts.Count == 0
            || milestoneAmounts.All(x => x.PaymentStatus ==
                (byte)ContractPaymentMilestoneStatus.Paid);
        var paidForEvaluation = allStructuredMilestonesPaid ? paid : 0m;
        var evaluation = ContractCompletionPolicy.Evaluate((ContractStatus)contract.Status,
            signed, acceptance, version.TotalAmount, paidForEvaluation);
        var visibleBlockers = evaluation.Blockers.Where(blocker =>
            contract.Status != (byte)ContractStatus.Completed
            || blocker.Code != ContractCompletionBlockerCode.ContractMustBeSigned);
        return new ContractCompletionReadinessResponse { Signed = signed && contract.Status is (byte)ContractStatus.Signed or (byte)ContractStatus.Completed,
            AcceptanceEvidenceAvailable = acceptance, TotalAmount = version.TotalAmount, PaidAmount = paid,
            RemainingAmount = version.TotalAmount - paid, CurrencyCode = version.CurrencyCode, Ready = evaluation.CanComplete,
            Blockers = visibleBlockers.Select(x => new ContractCompletionBlockerResponse { Code = x.Code switch {
                ContractCompletionBlockerCode.ContractMustBeSigned => "NOT_SIGNED", ContractCompletionBlockerCode.AcceptanceEvidenceMissing => "ACCEPTANCE_MISSING", _ => "PAYMENT_NOT_FULLY_PAID" },
                Message = x.Code switch { ContractCompletionBlockerCode.ContractMustBeSigned => "Hợp đồng chưa có bản ký hợp lệ.", ContractCompletionBlockerCode.AcceptanceEvidenceMissing => "Chưa tải biên bản nghiệm thu.", _ => "Hợp đồng chưa được thanh toán đủ." } }).ToList() };
    }

    private async Task<(TblContract Contract, TblContractVersion Version)> LoadWritableStateAsync(int contractId, int versionId, string contractRv, string versionRv, CancellationToken ct)
    {
        var contract = await _db.TblContracts.SingleOrDefaultAsync(x => x.ContractId == contractId, ct) ?? throw new KeyNotFoundException("Không tìm thấy hợp đồng.");
        Match(contract.RowVersion, Decode(contractRv), "Hợp đồng");
        if (contract.CurrentVersionId != versionId) throw Rule("CompletionStateChanged", "Version hiện hành đã thay đổi.", StatusCodes.Status409Conflict);
        var version = await _db.TblContractVersions.SingleOrDefaultAsync(x => x.VersionId == versionId && x.ContractId == contractId, ct) ?? throw new KeyNotFoundException("Không tìm thấy version hợp đồng.");
        Match(version.RowVersion, Decode(versionRv), "Version hợp đồng");
        return (contract, version);
    }

    private async Task<T> ExecuteInTransactionAsync<T>(
        Func<Task<T>> operation,
        CancellationToken cancellationToken)
    {
        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);
            try
            {
                var result = await operation();
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                _db.ChangeTracker.Clear();
                throw;
            }
        });
    }

    private static void EnsureSigned(TblContract contract, TblContractVersion version)
    {
        if (contract.Status != (byte)ContractStatus.Signed || !version.IsLocked)
            throw Rule("ContractMustBeSigned", "Chỉ hợp đồng Đã ký mới được cập nhật hồ sơ hoàn tất.", StatusCodes.Status409Conflict);
    }

    private async Task<StoredPrivateFile> SaveFileAsync(IFormFile file, string objectType, int contractId, CancellationToken ct)
    {
        var tenant = _tenant.GetRequiredTenant();
        await using var content = file.OpenReadStream();
        return await _files.SaveAsync(new(content, file.FileName, file.ContentType, file.Length,
            tenant.TenantCode, objectType, contractId, PrivateFileUploadPolicies.ContractEvidence()), ct);
    }

    private static TblFileStorage CreateFile(StoredPrivateFile file, string objectType, int contractId, int employeeId) => new()
    { ObjectType = objectType, ObjectId = contractId, FileName = file.OriginalFileName, FilePath = string.Empty,
      StorageKey = file.StorageKey, ContentType = file.ContentType, Sha256 = file.Sha256, TenantCode = file.TenantCode,
      FileType = Path.GetExtension(file.OriginalFileName).TrimStart('.').ToLowerInvariant(), FileSize = file.FileSize,
      UploadedByUserId = employeeId, UploadedDate = file.CreatedAt };

    private async Task<ContractAcceptanceEvidenceResponse> LoadAcceptanceAsync(int id, CancellationToken ct)
    {
        var row = await (from e in _db.TblContractAcceptanceEvidences.AsNoTracking() join f in _db.TblFileStorages.AsNoTracking() on e.FileId equals f.FileId
            join v in _db.TblContractVersions.AsNoTracking() on e.VersionId equals v.VersionId join u in _db.TblEmployees.AsNoTracking() on e.UploadedByEmployeeId equals u.EmployeeId
            where e.AcceptanceEvidenceId == id select new { e, f, v.VersionNo, u.EmployeeFullName }).SingleAsync(ct);
        return new() { AcceptanceEvidenceId = id, ContractId = row.e.ContractId, VersionId = row.e.VersionId, VersionNo = row.VersionNo,
            FileId = row.f.FileId, FileName = row.f.FileName, FileType = row.f.FileType ?? "", ContentType = row.f.ContentType ?? "", FileSize = row.f.FileSize ?? 0,
            Sha256 = row.f.Sha256 ?? "", UploadedByEmployeeId = row.e.UploadedByEmployeeId, UploadedByEmployeeName = row.EmployeeFullName,
            UploadedAt = row.e.UploadedAt, RowVersion = Encode(row.e.RowVersion) };
    }

    private async Task<ContractPaymentResponse> LoadPaymentAsync(int id, CancellationToken ct)
    {
        var p = await _db.TblContractPaymentLedgers.AsNoTracking().SingleAsync(x => x.ContractPaymentId == id, ct);
        var versionNo = await _db.TblContractVersions.AsNoTracking().Where(x => x.VersionId == p.VersionId).Select(x => x.VersionNo).SingleAsync(ct);
        var creator = await _db.TblEmployees.AsNoTracking().Where(x => x.EmployeeId == p.CreatedByEmployeeId).Select(x => x.EmployeeFullName).SingleOrDefaultAsync(ct);
        var voider = p.VoidedByEmployeeId.HasValue ? await _db.TblEmployees.AsNoTracking().Where(x => x.EmployeeId == p.VoidedByEmployeeId).Select(x => x.EmployeeFullName).SingleOrDefaultAsync(ct) : null;
        var fileName = p.EvidenceFileId.HasValue ? await _db.TblFileStorages.AsNoTracking().Where(x => x.FileId == p.EvidenceFileId).Select(x => x.FileName).SingleOrDefaultAsync(ct) : null;
        return new() { ContractPaymentId = id, ContractId = p.ContractId, VersionId = p.VersionId, VersionNo = versionNo,
            PaymentMilestoneId = p.PaymentMilestoneId, PaymentDate = p.PaymentDate,
            Amount = p.Amount, CurrencyCode = p.CurrencyCode, PaymentMethod = p.PaymentMethod, ReferenceCode = p.ReferenceCode,
            EvidenceFileId = p.EvidenceFileId, EvidenceFileName = fileName, Status = (ContractPaymentStatus)p.Status, CreatedByEmployeeId = p.CreatedByEmployeeId,
            CreatedByEmployeeName = creator, CreatedAt = p.CreatedAt, VoidReason = p.VoidReason, VoidedByEmployeeId = p.VoidedByEmployeeId,
            VoidedByEmployeeName = voider, VoidedAt = p.VoidedAt, RowVersion = Encode(p.RowVersion) };
    }

    private async Task ActivateMilestonesAsync(int versionId, PaymentDueAnchor anchor,
        DateTime anchorDate, int employeeId, CancellationToken ct)
    {
        var rows = await _db.TblContractPaymentMilestones.Where(x =>
            x.VersionId == versionId && x.DueAnchor == (byte)anchor
            && x.AnchorDate == null).ToListAsync(ct);
        var now = DateTime.UtcNow;
        foreach (var row in rows)
        {
            row.AnchorDate = anchorDate;
            row.DueDate = _paymentDueDates.Calculate(anchorDate, row.DueOffsetDays,
                (PaymentDayCountMode)row.DayCountMode);
            row.UpdatedEmployeeId = employeeId;
            row.UpdatedDate = now;
        }
        if (rows.Count > 0) await _db.SaveChangesAsync(ct);
    }

    private static IReadOnlyDictionary<string, object?> PaymentAudit(TblContractPaymentLedger p, decimal paid, decimal remaining) => ContractAuditValues.Create(
        ("ContractPaymentId", p.ContractPaymentId), ("CurrentVersionId", p.VersionId),
        ("PaymentMilestoneId", p.PaymentMilestoneId), ("PaymentDate", p.PaymentDate), ("Amount", p.Amount),
        ("CurrencyCode", p.CurrencyCode), ("PaymentMethod", p.PaymentMethod), ("ReferenceCode", p.ReferenceCode), ("EvidenceFileId", p.EvidenceFileId),
        ("PaymentStatus", p.Status), ("PaidAmount", paid), ("RemainingAmount", remaining));
    private static string Required(string? value, int max, string label) { var v = value?.Trim(); if (string.IsNullOrWhiteSpace(v)) throw Rule("RequiredField", $"{label} là bắt buộc."); if (v.Length > max) throw Rule("FieldTooLong", $"{label} không được vượt quá {max} ký tự."); return v; }
    private static byte[] Decode(string value) { try { var bytes = Convert.FromBase64String(value); return bytes.Length > 0 ? bytes : throw new FormatException(); } catch { throw Rule("StaleRowVersion", "RowVersion không hợp lệ.", StatusCodes.Status409Conflict); } }
    private static void Match(byte[] current, byte[] expected, string name) { if (!current.AsSpan().SequenceEqual(expected)) throw new DbUpdateConcurrencyException($"{name} đã thay đổi."); }
    private static string Encode(byte[]? value) => Convert.ToBase64String(value ?? []);
    private static BusinessRuleException Rule(string code, string message, int status = StatusCodes.Status400BadRequest) => new(status, code, message);
}
