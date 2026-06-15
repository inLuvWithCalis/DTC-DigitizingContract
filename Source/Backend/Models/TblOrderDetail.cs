using System;
using System.Collections.Generic;

namespace ContractManagement.Models;

public partial class TblOrderDetail
{
    public int OrderDetailsId { get; set; }

    public int? OrderId { get; set; }

    public int? ProductId { get; set; }

    public double? OrderQuantity { get; set; }

    public double? UnitPrice { get; set; }

    public DateTime? DateCreated { get; set; }

    public DateTime? DateExpired { get; set; }

    public byte? Status { get; set; }

    public byte? ItemType { get; set; }

    public DateTime? StartDate { get; set; }

    public string? NameDetails { get; set; }

    public short? ItemGroupId { get; set; }

    public double? DiscountPercent { get; set; }
}
