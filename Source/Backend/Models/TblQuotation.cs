using System;
using System.Collections.Generic;

namespace ContractManagement.Models;

public partial class TblQuotation
{
    public int QuotationId { get; set; }

    public int CustomerId { get; set; }

    public string QuotationNo { get; set; } = null!;

    public DateTime QuotationDate { get; set; }

    public decimal TotalAmount { get; set; }

    public string? QuatationStatus { get; set; }

    public int? CreatedEmployeeId { get; set; }
}
