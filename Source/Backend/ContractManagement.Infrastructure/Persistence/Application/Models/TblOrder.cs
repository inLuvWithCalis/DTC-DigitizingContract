using System;
using System.Collections.Generic;

namespace ContractManagement.Infrastructure.Persistence.Application.Models;

public partial class TblOrder
{
    public int OrderId { get; set; }

    public int? UserId { get; set; }

    public int? ContractId { get; set; }

    public DateTime? DateCreated { get; set; }

    public DateTime? DateExpired { get; set; }

    /// <summary>
    /// 0: Pending, 1: Approved
    /// </summary>
    public byte? OrderStatus { get; set; }

    public byte? OrderType { get; set; }

    public string? OrderComment { get; set; }

    public string? NoteFromAdmin { get; set; }

    public int? UpdatedUser { get; set; }

    public DateTime? UpdatedDate { get; set; }
}
