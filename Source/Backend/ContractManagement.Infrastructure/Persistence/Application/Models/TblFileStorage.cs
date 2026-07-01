using System;
using System.Collections.Generic;

namespace ContractManagement.Infrastructure.Persistence.Application.Models;

public partial class TblFileStorage
{
    public int FileId { get; set; }

    public string ObjectType { get; set; } = null!;

    public int ObjectId { get; set; }

    public string FileName { get; set; } = null!;

    public string FilePath { get; set; } = null!;

    public string? FileType { get; set; }

    public long? FileSize { get; set; }

    public int? UploadedByUserId { get; set; }

    public DateTime UploadedDate { get; set; }
}
