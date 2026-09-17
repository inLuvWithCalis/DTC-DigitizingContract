using System;
using System.Collections.Generic;

namespace ContractManagement.Infrastructure.Persistence.Application.Models;

public partial class TblContractAppendix
{
    public int AppendixId { get; set; }

    public int ContractId { get; set; }

    public int VersionId { get; set; }

    public int SourceTemplateAppendixId { get; set; }

    public string AppendixCode { get; set; } = null!;

    public string AppendixName { get; set; } = null!;

    public string? AppendixNameEn { get; set; }

    public string? AppendixDescription { get; set; }

    public bool IsRequired { get; set; }

    public int DisplayOrder { get; set; }

    public int CreatedEmployeeId { get; set; }

    public DateTime CreatedDate { get; set; }

    public int? UpdatedEmployeeId { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public byte[] RowVersion { get; set; } = null!;
}
