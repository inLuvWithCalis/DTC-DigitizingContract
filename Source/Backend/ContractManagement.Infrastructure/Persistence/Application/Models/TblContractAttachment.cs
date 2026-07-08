using System;
using System.Collections.Generic;

namespace ContractManagement.Infrastructure.Persistence.Application.Models;

public partial class TblContractAttachment
{
    public int AttachmentId { get; set; }

    public int ContractId { get; set; }

    public string? ContractFileName { get; set; }

    public string? ContractFilePath { get; set; }

    public DateTime? UploadDate { get; set; }

    public int? UploadEmployeeId { get; set; }

    /// <summary>
    /// Loại tài liệu đính kèm.
    /// Ví dụ:
    /// 0 = QuotationFile
    /// 1 = AcceptanceRecord
    /// 2 = HandoverRecord
    /// 3 = LiquidationRecord
    /// 4 = VATInvoice
    /// 5 = BankGuarantee
    /// 6 = SignedScanCopy
    /// 99 = Other
    /// </summary>
    public byte DocumentType { get; set; } = 99;
}
