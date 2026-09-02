namespace ContractManagement.Infrastructure.DatabaseScripts.SeedData;

public sealed class SystemAdminBootstrapOptions
{
    public const string SectionName = "SystemAdminBootstrap";

    public bool Enabled { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
}
