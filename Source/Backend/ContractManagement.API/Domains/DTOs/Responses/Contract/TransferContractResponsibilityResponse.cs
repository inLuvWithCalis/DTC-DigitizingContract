namespace ContractManagement.API.Domains.DTOs.Responses.Contract;

public class TransferContractResponsibilityResponse
{
    public int ContractId { get; set; }

    public int PreviousResponsibleEmployeeId { get; set; }

    public int ResponsibleEmployeeId { get; set; }

    public int TransferredByEmployeeId { get; set; }

    public DateTime TransferredAt { get; set; }

    public string RowVersion { get; set; } = string.Empty;
}
