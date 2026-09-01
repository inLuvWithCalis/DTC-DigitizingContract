namespace ContractManagement.Infrastructure.Persistence.Application.Models;

/// <summary>
/// Khoản thanh toán của một ContractVersion trong lifecycle MVP.
/// Không dùng chung với tbl_Payment legacy gắn Invoice.
/// </summary>
public sealed class TblContractPaymentLedger
{
    public int ContractPaymentId { get; set; }
    public int ContractId { get; set; }
    public int VersionId { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "VND";
    public string PaymentMethod { get; set; } = null!;
    public string ReferenceCode { get; set; } = null!;
    public int? EvidenceFileId { get; set; }
    public byte Status { get; set; }
    public int CreatedByEmployeeId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? VoidReason { get; set; }
    public int? VoidedByEmployeeId { get; set; }
    public DateTime? VoidedAt { get; set; }
    public byte[] RowVersion { get; set; } = null!;
}
