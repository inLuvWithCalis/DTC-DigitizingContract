using System;
using System.Collections.Generic;

namespace ContractManagement.Infrastructure.Persistence.Application.Models;

public partial class TblApprovalHistory
{
    public int ApprovalHistoryId { get; set; }

    public int? WorkflowId { get; set; }

    public string ObjectType { get; set; } = null!;

    public int ObjectId { get; set; }

    public int StepNo { get; set; }

    public int ApproverEmployeeId { get; set; }

    public string ApprovalAction { get; set; } = null!;

    public string? Comment { get; set; }

    public DateTime ActionDate { get; set; }
}
