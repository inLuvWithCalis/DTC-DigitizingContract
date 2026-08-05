using ContractManagement.API.Common.Enums;

namespace ContractManagement.API.Domains.DTOs.Responses.Contract;

public sealed class CreateContractNegotiationRoundResponse
{
    public int ContractId { get; set; }

    public ContractStatus Status { get; set; }

    public string RowVersion { get; set; } = string.Empty;

    public ContractNegotiationRoundVersionResponse SourceVersion
    {
        get;
        set;
    } = new();

    public ContractNegotiationRoundVersionResponse CurrentVersion
    {
        get;
        set;
    } = new();

    public ContractFinancialTotalsResponse Totals { get; set; } = new();
}

public sealed class ContractNegotiationRoundVersionResponse
{
    public int VersionId { get; set; }

    public int VersionNo { get; set; }

    public int? SourceVersionId { get; set; }

    public bool IsLocked { get; set; }

    public DateTime? LockedDate { get; set; }

    public string? SnapshotHash { get; set; }

    public string RowVersion { get; set; } = string.Empty;
}

public sealed class ContractFinancialTotalsResponse
{
    public string CurrencyCode { get; set; } = string.Empty;

    public decimal Subtotal { get; set; }

    public decimal TotalDiscount { get; set; }

    public decimal TotalVat { get; set; }

    public decimal TotalPayment { get; set; }
}
