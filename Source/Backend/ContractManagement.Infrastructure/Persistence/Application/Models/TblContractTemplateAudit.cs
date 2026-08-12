namespace ContractManagement.Infrastructure.Persistence.Application.Models;

/// <summary>
/// Audit append-only của thao tác DOCX TemplateVersion trong một tenant.
/// Chỉ lưu metadata đã safelist; không bao giờ giữ tên file gốc hay nội dung DOCX.
/// </summary>
public partial class TblContractTemplateAudit
{
    public int ContractTemplateAuditId { get; set; }

    public int TenantId { get; set; }

    public int TemplateId { get; set; }

    public int TemplateVersionId { get; set; }

    public int ActorEmployeeId { get; set; }

    public string ActionType { get; set; } = null!;

    public string Result { get; set; } = null!;

    public string? FailureCode { get; set; }

    public string? PreviousValuesJson { get; set; }

    public string? NewValuesJson { get; set; }

    public DateTime OccurredAt { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public string CorrelationId { get; set; } = null!;
}
