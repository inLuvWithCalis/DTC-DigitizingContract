using System;
using System.Collections.Generic;

namespace ContractManagement.Infrastructure.Persistence.Application.Models;

public partial class TblPaymentSchedule
{
    public int ScheduleId { get; set; }

    public int ContractId { get; set; }

    public DateTime DueDate { get; set; }

    public double Amount { get; set; }

    public double PaidAmount { get; set; }

    public string PaymentStatus { get; set; } = null!;

    public string? Note { get; set; }
}
