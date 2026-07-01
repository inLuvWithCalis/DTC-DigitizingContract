using System;
using System.Collections.Generic;

namespace ContractManagement.Infrastructure.Persistence.Application.Models;

public partial class TblDeliveryOrder
{
    public int DeliveryId { get; set; }

    public int OrderId { get; set; }

    public string DeliveryNo { get; set; } = null!;

    public DateTime DeliveryDate { get; set; }

    public string? DeliveryAddress { get; set; }

    public string? ReceiverName { get; set; }

    public string? ReceiverPhone { get; set; }

    public string DeliveryStatus { get; set; } = null!;
}
