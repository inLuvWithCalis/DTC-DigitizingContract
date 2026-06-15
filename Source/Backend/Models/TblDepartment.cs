using System;
using System.Collections.Generic;

namespace ContractManagement.Models;

public partial class TblDepartment
{
    public short DepartmentId { get; set; }

    public string DepartmentCode { get; set; } = null!;

    public string DepartmentName { get; set; } = null!;

    public DateTime? ModifiedDate { get; set; }

    public byte? Stutus { get; set; }

    public int? LangId { get; set; }
}
