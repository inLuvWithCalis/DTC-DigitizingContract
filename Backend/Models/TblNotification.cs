using System;
using System.Collections.Generic;

namespace ContractManagement.Models;

public partial class TblNotification
{
    public int NotificationId { get; set; }

    public int UserId { get; set; }

    public string NotificationTitle { get; set; } = null!;

    public string? NotificationMessage { get; set; }

    public string? ObjectType { get; set; }

    public int? ObjectId { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedDate { get; set; }
}
