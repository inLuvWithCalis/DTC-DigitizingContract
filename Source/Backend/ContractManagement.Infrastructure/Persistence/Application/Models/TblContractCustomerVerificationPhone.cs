namespace ContractManagement.Infrastructure.Persistence.Application.Models;

/// <summary>
/// Immutable history of a phone number selected to verify customer access.
/// The normalized number is never returned by public APIs or written to audits.
/// </summary>
public sealed class TblContractCustomerVerificationPhone
{
    public int VerificationPhoneId { get; set; }

    public int ContractId { get; set; }

    public string PhoneSource { get; set; } = null!;

    public string PhoneNumberNormalized { get; set; } = null!;

    public string Reason { get; set; } = null!;

    public int CreatedByEmployeeId { get; set; }

    public DateTime CreatedDate { get; set; }

    public byte[] RowVersion { get; set; } = null!;
}
