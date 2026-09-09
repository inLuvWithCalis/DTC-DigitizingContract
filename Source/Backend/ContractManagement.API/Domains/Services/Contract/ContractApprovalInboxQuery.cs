using ContractManagement.API.Common.Enums;
using ContractManagement.Infrastructure.Persistence.Application;
using ContractManagement.Infrastructure.Persistence.Application.Models;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Domains.Services.Contract;

internal static class ContractApprovalInboxQuery
{
    public static async Task<IQueryable<TblContractApprovalRequest>> CreateAsync(
        DbDtctechContext dbContext,
        int managerEmployeeId,
        CancellationToken cancellationToken)
    {
        var allowedWorkflowIds = await dbContext.TblApprovalWorkflows
            .AsNoTracking()
            .Where(workflow =>
                workflow.IsActive
                && workflow.ObjectType == "Contract"
                && workflow.StepNo == 1
                && (!workflow.ApproverEmployeeId.HasValue
                    || workflow.ApproverEmployeeId.Value == managerEmployeeId))
            .Select(workflow => workflow.WorkflowId)
            .ToListAsync(cancellationToken);

        return dbContext.TblContractApprovalRequests
            .AsNoTracking()
            .Where(request =>
                request.Status == (byte)ApprovalRequestStatus.Pending
                && request.SubmittedByEmployeeId != managerEmployeeId
                && (!request.WorkflowId.HasValue
                    || allowedWorkflowIds.Contains(request.WorkflowId.Value)));
    }
}
