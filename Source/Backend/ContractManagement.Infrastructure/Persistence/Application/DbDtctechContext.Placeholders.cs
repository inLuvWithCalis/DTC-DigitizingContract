using ContractManagement.Infrastructure.Persistence.Application.Models;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Infrastructure.Persistence.Application;

public partial class DbDtctechContext
{
    public DbSet<TblContractPlaceholderDefinition> TblContractPlaceholderDefinitions { get; set; }
    public DbSet<TblContractPlaceholderAudit> TblContractPlaceholderAudits { get; set; }
    public DbSet<TblContractVersionPlaceholderValue> TblContractVersionPlaceholderValues { get; set; }

    private static void ConfigurePlaceholders(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TblContractTemplateVersion>().Property(x => x.PlaceholderBindingHash).HasMaxLength(64).IsUnicode(false);
        modelBuilder.Entity<TblContractPlaceholderDefinition>(e =>
        {
            e.ToTable("tbl_ContractPlaceholderDefinition");
            e.HasKey(x => x.PlaceholderDefinitionId);
            e.HasIndex(x => x.PlaceholderKey).IsUnique();
            e.Property(x => x.PlaceholderKey).HasMaxLength(100).IsUnicode(false);
            e.Property(x => x.FieldLabel).HasMaxLength(300);
            e.Property(x => x.SourceFieldKey).HasMaxLength(200).IsUnicode(false);
            e.Property(x => x.DefaultValue).HasMaxLength(2000);
            e.Property(x => x.FormatString).HasMaxLength(100).IsUnicode(false);
            e.Property(x => x.RowVersion).IsRowVersion();
        });
        modelBuilder.Entity<TblContractTemplateField>(e =>
        {
            e.Property(x => x.SourceFieldKey).HasMaxLength(200).IsUnicode(false);
            e.Property(x => x.DefinitionRowVersion).HasMaxLength(8);
        });
        modelBuilder.Entity<TblContractVersionPlaceholderValue>(e =>
        {
            e.ToTable("tbl_ContractVersionPlaceholderValue");
            e.HasKey(x => x.ContractVersionPlaceholderValueId);
            e.HasIndex(x => new { x.VersionId, x.PlaceholderKey }).IsUnique();
            e.HasIndex(x => new { x.ContractId, x.VersionId });
            e.Property(x => x.PlaceholderKey).HasMaxLength(100).IsUnicode(false);
            e.Property(x => x.SourceFieldKey).HasMaxLength(200).IsUnicode(false);
        });
        modelBuilder.Entity<TblContractPlaceholderAudit>(e =>
        {
            e.ToTable("tbl_ContractPlaceholderAudit");
            e.HasKey(x => x.PlaceholderAuditId);
            e.HasIndex(x => new { x.PlaceholderKey, x.OccurredAt });
            e.Property(x => x.PlaceholderKey).HasMaxLength(100).IsUnicode(false);
            e.Property(x => x.ActionType).HasMaxLength(64).IsUnicode(false);
        });
    }
}
