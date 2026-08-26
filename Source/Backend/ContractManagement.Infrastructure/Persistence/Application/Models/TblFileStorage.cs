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

    /// <summary>
    /// Identifier tương đối trong private storage. Không phải URL hoặc physical path.
    /// Nullable để giữ tương thích với file legacy đang dùng FilePath.
    /// </summary>
    public string? StorageKey { get; set; }

    public string? ContentType { get; set; }

    public string? Sha256 { get; set; }

    public string? TenantCode { get; set; }

    public string? FileType { get; set; }

    public long? FileSize { get; set; }

    public int? UploadedByUserId { get; set; }

    public DateTime UploadedDate { get; set; }
}
