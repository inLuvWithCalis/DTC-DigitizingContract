using System;
using System.Collections.Generic;

namespace ContractManagement.Infrastructure.Persistence.Application.Models;

public partial class TblContractTerm
{
    public int TermId { get; set; }

    public int ContractId { get; set; }

    public string TermTitle { get; set; } = null!;

    public string? TermContent { get; set; }

    public int? DisplayOrder { get; set; }
}
