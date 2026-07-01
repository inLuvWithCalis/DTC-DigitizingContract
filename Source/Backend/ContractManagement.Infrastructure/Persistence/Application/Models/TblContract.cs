using System;
using System.Collections.Generic;

namespace ContractManagement.Infrastructure.Persistence.Application.Models;

public partial class TblContract
{
    public int ContractId { get; set; }

    public int? CustomerId { get; set; }

    public int? EmployeeId { get; set; }

    public string? ContractCode { get; set; }

    public string? ContractName { get; set; }

    public string? ContractNameEn { get; set; }

    public DateTime? SignDate { get; set; }

    public DateTime? EffectiveDate { get; set; }

    public DateTime? ExpireDate { get; set; }

    public byte? Status { get; set; }

    public double? TotalAmount { get; set; }

    public int? CreatedEmployeeId { get; set; }

    public int? UpdatedEmployeeId { get; set; }

    public DateTime? CreatedDate { get; set; }

    public DateTime? UpdateDate { get; set; }
}
