using System;
using System.Collections.Generic;

namespace ContractManagement.Models;

public partial class TblQuotationDetail
{
    public int QuotationDetailId { get; set; }

    public int QuotationId { get; set; }

    public int ProductId { get; set; }

    public int? Quantity { get; set; }

    public decimal? UnitPrice { get; set; }

    public decimal? Amount { get; set; }
}
