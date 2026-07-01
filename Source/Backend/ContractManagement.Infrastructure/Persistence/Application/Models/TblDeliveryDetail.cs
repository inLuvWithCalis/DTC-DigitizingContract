using System;
using System.Collections.Generic;

namespace ContractManagement.Infrastructure.Persistence.Application.Models;

public partial class TblDeliveryDetail
{
    public int DeliveryDetailId { get; set; }

    public int DeliveryId { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public string? Note { get; set; }
}
