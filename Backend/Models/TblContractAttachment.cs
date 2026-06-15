using System;
using System.Collections.Generic;

namespace ContractManagement.Models;

public partial class TblContractAttachment
{
    public int AttachmentId { get; set; }

    public int ContractId { get; set; }

    public string? ContractFileName { get; set; }

    public string? ContractFilePath { get; set; }

    public DateTime? UploadDate { get; set; }

    public int? UploadEmployeeId { get; set; }
}
