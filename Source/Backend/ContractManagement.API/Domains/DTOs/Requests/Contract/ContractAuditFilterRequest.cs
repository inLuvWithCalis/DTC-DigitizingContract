namespace ContractManagement.API.Domains.DTOs.Requests.Contract;

/// <summary>
/// Bộ lọc nhật ký hoạt động Contract. Thời gian phải truyền UTC, ví dụ
/// <c>2026-08-07T00:00:00Z</c>.
/// </summary>
public sealed class ContractAuditFilterRequest
{
    public int? ContractId { get; set; }

    public int? VersionId { get; set; }

    public string? ActorType { get; set; }

    public string? ActionType { get; set; }

    public string? Result { get; set; }

    public DateTime? FromUtc { get; set; }

    public DateTime? ToUtc { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;
}
