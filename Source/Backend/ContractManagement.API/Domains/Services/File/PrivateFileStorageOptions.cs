namespace ContractManagement.Domains.Services.File;

public sealed class PrivateFileStorageOptions
{
    public const string SectionName = "PrivateFileStorage";

    /// <summary>
    /// Đường dẫn tuyệt đối hoặc tương đối với ContentRoot. Không được nằm trong wwwroot.
    /// </summary>
    public string RootPath { get; set; } = "../private-storage";
}
