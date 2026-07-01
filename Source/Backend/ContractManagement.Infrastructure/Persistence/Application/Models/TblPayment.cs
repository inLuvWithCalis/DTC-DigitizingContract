using System;
using System.Collections.Generic;

namespace ContractManagement.Infrastructure.Persistence.Application.Models;

public partial class TblPayment
{
    public int PaymentId { get; set; }

    public int InvoiceId { get; set; }

    public DateTime PaymentDate { get; set; }

    public double Amount { get; set; }

    public string? PaymentMethod { get; set; }

    public string? ReferenceNo { get; set; }
}
