using System;
using System.Collections.Generic;

namespace ContractManagement.Infrastructure.Persistence.Application.Models;

public partial class TblServiceType
{
    public byte ServiceTypeId { get; set; }

    public string? ServiceTypeName { get; set; }

    public byte? LangId { get; set; }
}
