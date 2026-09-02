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

    public DateTime? UpdatedAt { get; set; }

    public DateTime? PasswordChangedAt { get; set; }

    public bool MustChangePassword { get; set; }

    public int SessionVersion { get; set; } = 1;

    public string? AvatarStorageKey { get; set; }

    public string? AvatarContentType { get; set; }

    public long? AvatarFileSize { get; set; }

    public string? AvatarSha256 { get; set; }

    public DateTime? AvatarUpdatedAt { get; set; }

    public string? CoverStorageKey { get; set; }

    public string? CoverContentType { get; set; }

    public long? CoverFileSize { get; set; }

    public string? CoverSha256 { get; set; }

    public DateTime? CoverUpdatedAt { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
