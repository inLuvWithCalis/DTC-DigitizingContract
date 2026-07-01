using System;
using System.Collections.Generic;

namespace ContractManagement.Infrastructure.Persistence.Application.Models;

public partial class TblCustomerInteraction
{
    public int InteractionId { get; set; }

    public int CustomerId { get; set; }

    public int EmployeeId { get; set; }

    public DateTime InteractionDate { get; set; }

    public string InteractionType { get; set; } = null!;

    public string? InteractionSubject { get; set; }

    public string? Content { get; set; }

    public DateTime? NextFollowUpDate { get; set; }
}
