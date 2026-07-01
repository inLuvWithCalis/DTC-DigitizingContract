using System;
using System.Collections.Generic;

namespace ContractManagement.Infrastructure.Persistence.Application.Models;

public partial class TblCategory
{
    public byte CategoryId { get; set; }

    public string? CategoryName { get; set; }

    public string? CategoryShortDesc { get; set; }

    public byte? CategoryOrder { get; set; }

    public byte? CategoryParentId { get; set; }

    public int? LangId { get; set; }

    public string? Image { get; set; }
}
