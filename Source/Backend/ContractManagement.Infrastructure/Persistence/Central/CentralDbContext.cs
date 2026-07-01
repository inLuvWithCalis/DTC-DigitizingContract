using ContractManagement.Infrastructure.Persistence.Central.Entities;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Infrastructure.Persistence.Central;

/// <summary>
/// DbContext quản lý Central Database.
///
/// Context lưu tenant và thông tin database.
/// </summary>
public sealed class CentralDbContext : DbContext
{
    public CentralDbContext(
        DbContextOptions<CentralDbContext> options)
        : base(options)
    {
    }

    public DbSet<SystemAdmin> SystemAdmins => Set<SystemAdmin>();

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<TenantDatabase> TenantDatabases =>
        Set<TenantDatabase>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureTenant(modelBuilder);

        ConfigureTenantDatabase(modelBuilder);

        ConfigureSystemAdmin(modelBuilder); // Configure SystemAdmin entity
    }

    /*
     *  Cấu hình entity SystemAdmin.
     */
    private static void ConfigureSystemAdmin(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SystemAdmin>(entity =>
        {
            entity.ToTable("SystemAdmins");

            entity.HasKey(x => x.SystemAdminId);

            entity.Property(x => x.Username)
                .HasMaxLength(100)
                .IsUnicode(false)
                .IsRequired();

            entity.Property(x => x.PasswordHash)
                .HasMaxLength(255)
                .IsUnicode(false)
                .IsRequired();

            entity.Property(x => x.FullName)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(x => x.Email)
                .HasMaxLength(200)
                .IsUnicode(false);

            entity.Property(x => x.IsActive)
                .HasDefaultValue(true);

            entity.Property(x => x.CreatedAt)
                .HasColumnType("datetime2");

            entity.HasIndex(x => x.Username)
                .IsUnique();
        });
    }

    private static void ConfigureTenant(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.ToTable("Tenants");

            entity.HasKey(x => x.TenantId);

            entity.Property(x => x.TenantCode)
                .HasMaxLength(50)
                .IsUnicode(false)
                .IsRequired();

            entity.Property(x => x.TenantName)
                .HasMaxLength(200)
                .IsRequired();

            /*
             * Enum sẽ được lưu thành:
             *
             * Active
             * Failed
             * Suspended
             *
             * thay vì 1, 2, 3.
             */
            entity.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(x => x.ProvisioningError)
                .HasMaxLength(2000);

            entity.Property(x => x.CreatedAt)
                .HasColumnType("datetime2");

            entity.Property(x => x.UpdatedAt)
                .HasColumnType("datetime2");

            /*
             * Không cho phép hai tenant cùng TenantCode.
             */
            entity.HasIndex(x => x.TenantCode)
                .IsUnique();

            entity.HasOne(x => x.TenantDatabase)
                .WithMany(x => x.Tenants)
                .HasForeignKey(x => x.TenantDatabaseId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureTenantDatabase(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TenantDatabase>(entity =>
        {
            entity.ToTable("TenantDatabases");

            entity.HasKey(x => x.TenantDatabaseId);

            entity.Property(x => x.DatabaseKey)
                .HasMaxLength(100)
                .IsUnicode(false)
                .IsRequired();

            entity.Property(x => x.DatabaseName)
                .HasMaxLength(128)
                .IsUnicode(false)
                .IsRequired();

            entity.Property(x => x.ConnectionString)
                .HasMaxLength(2000)
                .IsRequired();

            entity.Property(x => x.Mode)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(x => x.CreatedAt)
                .HasColumnType("datetime2");

            entity.HasIndex(x => x.DatabaseKey)
                .IsUnique();

            entity.HasIndex(x => x.DatabaseName)
                .IsUnique();
        });
    }
}