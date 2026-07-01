namespace ContractManagement.Infrastructure.MultiTenancy.Options;

/// <summary>
/// Cấu hình multi-tenancy được đọc từ appsettings.json.
/// </summary>
public sealed class MultiTenancyOptions
{
    public const string SectionName = "MultiTenancy";

    /// <summary>
    /// Header dùng để test tenant.
    /// </summary>
    public string HeaderName { get; set; }
        = "X-Tenant-Code";

    /// <summary>
    /// Claim chứa tenant sau khi dùng Authentication.
    /// </summary>
    public string TenantClaimType { get; set; }
        = "tenant_code";

    /// <summary>
    /// Cho phép lấy tenant từ header.
    ///
    /// Production nên ưu tiên session hoặc claim.
    /// </summary>
    public bool AllowHeaderFallback { get; set; }
        = true;

    /// <summary>
    /// Tiền tố tên database mới.
    /// </summary>
    public string DatabasePrefix { get; set; }
        = "ContractManagement_Tenant_";

    /// <summary>
    /// Tên connection string mẫu trong appsettings.
    /// </summary>
    public string TemplateConnectionName { get; set; }
        = "TenantDatabaseTemplate";
}