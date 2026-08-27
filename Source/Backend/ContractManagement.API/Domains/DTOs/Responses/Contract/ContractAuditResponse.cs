using System.Text.Json;

namespace ContractManagement.API.Domains.DTOs.Responses.Contract;

/// <summary>
/// Một bản ghi audit đã được lọc an toàn cho nhân viên có quyền xem.
/// Các trường before/after chỉ chứa allowlist scalar, không có nội dung comment,
/// số điện thoại, token, OTP, cookie hay payload nội bộ.
/// </summary>
public sealed class ContractAuditResponse
{
    public int ContractAuditId { get; set; }

    public int ContractId { get; set; }

    public int? VersionId { get; set; }

    public string SubjectType { get; set; } = string.Empty;

    public int SubjectId { get; set; }

    public string ActorType { get; set; } = string.Empty;

    public int? ActorEmployeeId { get; set; }

    public int? ActorCustomerAccessSessionId { get; set; }

    /// <summary>
    /// Tên hiển thị của actor tại thời điểm truy vấn. ID actor vẫn được trả về
    /// để đối chiếu kỹ thuật khi tên không còn resolve được.
    /// </summary>
    public string? ActorDisplayName { get; set; }

    public string? ActorMaskedPhone { get; set; }

    public string? ActorPhoneSource { get; set; }

    public string? ContractCode { get; set; }

    public string? ContractName { get; set; }

    public int? VersionNo { get; set; }

    public string ActionType { get; set; } = string.Empty;

    public string Result { get; set; } = string.Empty;

    public string? FailureCode { get; set; }

    public Dictionary<string, JsonElement>? PreviousValues { get; set; }

    public Dictionary<string, JsonElement>? NewValues { get; set; }

    public string? Reason { get; set; }

    public DateTime OccurredAt { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public string CorrelationId { get; set; } = string.Empty;
}

public sealed class ContractAuditCursorPageResponse
{
    public List<ContractAuditResponse> Items { get; set; } = [];

    public int TotalCount { get; set; }

    public int PageSize { get; set; }

    public bool HasMore { get; set; }

    public string? NextCursor { get; set; }
}
