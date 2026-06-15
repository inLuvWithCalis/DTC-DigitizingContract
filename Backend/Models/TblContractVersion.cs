using System;
using System.Collections.Generic;

namespace ContractManagement.Models;

public partial class TblContractVersion
{
    public int VersionId { get; set; }

    public int ContractId { get; set; }

    public int VersionNo { get; set; }

    public string? ChangeNote { get; set; }

    public DateTime CreatedDate { get; set; }
}
