namespace ContractManagement.Domains.Services.File;

public sealed class PrivateFileStorageOptions
{
    public const string SectionName = "PrivateFileStorage";

    /// <summary>
    /// Đường dẫn tuyệt đối hoặc tương đối với ContentRoot. Không được nằm trong wwwroot.
    /// </summary>
    public string RootPath { get; set; } = "../private-storage";

    /// <summary>
    /// API từ chối khởi động khi ổ lưu trữ còn ít hơn ngưỡng này.
    /// Đặt 0 chỉ khi hạ tầng có cơ chế capacity check riêng.
    /// </summary>
    public long MinimumFreeSpaceBytes { get; set; } = 1024L * 1024 * 1024;
}
