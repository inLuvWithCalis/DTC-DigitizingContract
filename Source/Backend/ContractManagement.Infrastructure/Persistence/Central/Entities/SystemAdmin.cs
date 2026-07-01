namespace ContractManagement.Infrastructure.Persistence.Central.Entities;

public sealed class SystemAdmin
{
    public int SystemAdminId { get; set; }

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string? Email { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}