using System.ComponentModel.DataAnnotations;

namespace ContractManagement.API.Domains.DTOs.Requests.Contract;

public class SubmitContractForApprovalRequest
{
    [Required]
    public string RowVersion { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int CurrentVersionId { get; set; }

    [Required]
    public string CurrentVersionRowVersion { get; set; } =
        string.Empty;

    /// <summary>
    /// Có thể null nếu chưa cấu hình workflow duyệt.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int? WorkflowId { get; set; }
}