namespace ContractManagement.API.Domains.DTOs.Responses.Contract;

public sealed class ContractVersionHistoryResponse
{
    public int VersionId { get; set; }

    public int VersionNo { get; set; }

    public int? SourceVersionId { get; set; }

    public string? ChangeNote { get; set; }

    public bool IsLocked { get; set; }

    public DateTime? LockedDate { get; set; }

    public int? LockedByEmployeeId { get; set; }

    public int CreatedEmployeeId { get; set; }

    public DateTime CreatedDate { get; set; }

    public string RowVersion { get; set; } = string.Empty;
}
