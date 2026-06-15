using System;
using System.Collections.Generic;

namespace ContractManagement.Models;

public partial class TblInvoice
{
    public int InvoiceId { get; set; }

    public int OrderId { get; set; }

    public int? ContractId { get; set; }

    public string InvoiceNo { get; set; } = null!;

    public DateTime InvoiceDate { get; set; }

    public double TotalAmount { get; set; }

    public string? InvoiceStatus { get; set; }
}
