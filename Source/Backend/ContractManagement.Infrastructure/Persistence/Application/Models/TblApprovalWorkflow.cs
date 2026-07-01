using System;
using System.Collections.Generic;

namespace ContractManagement.Infrastructure.Persistence.Application.Models;

public partial class TblApprovalWorkflow
{
    public int WorkflowId { get; set; }

    public string WorkflowName { get; set; } = null!;

    public string ObjectType { get; set; } = null!;

    public int StepNo { get; set; }

    public int? ApproverRoleId { get; set; }

    public int? ApproverEmployeeId { get; set; }

    public bool IsActive { get; set; }
}
