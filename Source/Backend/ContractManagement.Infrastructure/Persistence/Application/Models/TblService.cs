using System;
using System.Collections.Generic;

namespace ContractManagement.Infrastructure.Persistence.Application.Models;

public partial class TblService
{
    public int ServiceId { get; set; }

    public string? ServiceName { get; set; }

    public int? ServiceParentId { get; set; }

    public int? Points { get; set; }

    public int? UserCreated { get; set; }

    public int? UserModified { get; set; }

    public DateTime? DateCreated { get; set; }

    public DateTime? DateModified { get; set; }

    public byte? Status { get; set; }

    public double? ServicePrice { get; set; }

    public int? DiskStorage { get; set; }

    public int? Bandwidth { get; set; }

    public short? SubDomain { get; set; }

    public short? EmailAccounts { get; set; }

    public byte? Ftpaccounts { get; set; }

    public byte? MySql { get; set; }

    public byte? MsSqlServer { get; set; }

    public int? ParkDomain { get; set; }

    /// <summary>
    /// 1 - Windows Hosting, 2 - Linux Hosting
    /// </summary>
    public byte? ServiceTypeId { get; set; }

    public byte? ServicePackageId { get; set; }

    public int? LangId { get; set; }

    public string? ServiceImageIcon { get; set; }

    public short? ServiceGroupId { get; set; }

    public double? SetupPrice { get; set; }

    public double? MaintainPrice { get; set; }

    public bool? HasChild { get; set; }

    public string? ServiceContent { get; set; }

    public int? ServiceOrder { get; set; }

    /// <summary>
    /// 1. Tên miền Việt Nam; 2. Tên miền quốc tế
    /// </summary>
    public byte? ServiceRegion { get; set; }

    public string? ServiceShortDesc { get; set; }

    public int? EmailForwarders { get; set; }

    public string? Others { get; set; }

    public string? Rewrite { get; set; }

    public string? MetaKeyword { get; set; }

    public string? MetaDescription { get; set; }

    public string? TitleBrowser { get; set; }
}
