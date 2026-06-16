using System;
using System.Collections.Generic;

namespace ContractManagement.Models;

public partial class TblQuotationDetail
{
    public int QuotationDetailId { get; set; }

    public int QuotationId { get; set; }

    public int ProductId { get; set; }

    public int? Quantity { get; set; }

    public double? UnitPrice { get; set; }

    public double? Amount { get; set; }
}
