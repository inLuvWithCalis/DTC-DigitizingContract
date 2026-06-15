using System;
using System.Collections.Generic;

namespace ContractManagement.Models;

public partial class TblContractAppendix
{
    public int AppendixId { get; set; }

    public int ContractId { get; set; }

    public string? AppendixCode { get; set; }

    public string? AppendixName { get; set; }

    public string? AppendixNameEn { get; set; }

    public DateTime? AppendixDate { get; set; }

    public string? AppendixDescription { get; set; }
}
