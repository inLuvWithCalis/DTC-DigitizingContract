using ContractManagement.Infrastructure.Persistence.Application.Models;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Infrastructure.Persistence.Application;

public partial class DbDtctechContext
{
    private static void ConfigureLegalBases(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TblContractTemplateLegalBasis>(entity =>
        {
            entity.HasKey(e => e.TemplateLegalBasisId)
                .HasName("PK_tbl_ContractTemplateLegalBasis");
            entity.ToTable("tbl_ContractTemplateLegalBasis", table =>
            {
                table.HasCheckConstraint("CK_tbl_ContractTemplateLegalBasis_TemplateVersionId", "[TemplateVersionId] > 0");
                table.HasCheckConstraint("CK_tbl_ContractTemplateLegalBasis_BasisCode", "LEN(LTRIM(RTRIM([BasisCode]))) > 0");
                table.HasCheckConstraint("CK_tbl_ContractTemplateLegalBasis_ContentVi", "LEN(LTRIM(RTRIM([ContentVi]))) > 0");
                table.HasCheckConstraint("CK_tbl_ContractTemplateLegalBasis_DisplayOrder", "[DisplayOrder] >= 0");
            });
            entity.HasIndex(e => new { e.TemplateVersionId, e.BasisCode })
                .IsUnique()
                .HasDatabaseName("UX_tbl_ContractTemplateLegalBasis_Version_BasisCode");
            entity.HasIndex(e => new { e.TemplateVersionId, e.DisplayOrder })
                .HasDatabaseName("IX_tbl_ContractTemplateLegalBasis_Version_DisplayOrder");
            entity.Property(e => e.BasisCode).HasMaxLength(100).IsUnicode(false).IsRequired();
            entity.Property(e => e.ContentVi).HasColumnType("nvarchar(max)").IsRequired();
            entity.Property(e => e.ContentEn).HasColumnType("nvarchar(max)");
            entity.Property(e => e.DisplayOrder).HasDefaultValue(0, "DF_tbl_ContractTemplateLegalBasis_DisplayOrder");
            entity.Property(e => e.CreatedDate).HasColumnType("datetime2").HasDefaultValueSql("(sysutcdatetime())", "DF_tbl_ContractTemplateLegalBasis_CreatedDate");
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime2");
            entity.Property(e => e.RowVersion).IsRowVersion().IsConcurrencyToken();
        });

        modelBuilder.Entity<TblContractLegalBasis>(entity =>
        {
            entity.HasKey(e => e.LegalBasisId)
                .HasName("PK_tbl_ContractLegalBasis");
            entity.ToTable("tbl_ContractLegalBasis", table =>
            {
                table.HasCheckConstraint("CK_tbl_ContractLegalBasis_ContractId", "[ContractId] > 0");
                table.HasCheckConstraint("CK_tbl_ContractLegalBasis_VersionId", "[VersionId] > 0");
                table.HasCheckConstraint("CK_tbl_ContractLegalBasis_SourceTemplateLegalBasisId", "[SourceTemplateLegalBasisId] IS NULL OR [SourceTemplateLegalBasisId] > 0");
                table.HasCheckConstraint("CK_tbl_ContractLegalBasis_BasisCode", "LEN(LTRIM(RTRIM([BasisCode]))) > 0");
                table.HasCheckConstraint("CK_tbl_ContractLegalBasis_ContentVi", "LEN(LTRIM(RTRIM([ContentVi]))) > 0");
                table.HasCheckConstraint("CK_tbl_ContractLegalBasis_DisplayOrder", "[DisplayOrder] >= 0");
            });
            entity.HasIndex(e => new { e.VersionId, e.BasisCode })
                .IsUnique()
                .HasDatabaseName("UX_tbl_ContractLegalBasis_Version_BasisCode");
            entity.HasIndex(e => new { e.ContractId, e.VersionId, e.DisplayOrder })
                .HasDatabaseName("IX_tbl_ContractLegalBasis_Contract_Version_DisplayOrder");
            entity.HasIndex(e => e.SourceTemplateLegalBasisId)
                .HasDatabaseName("IX_tbl_ContractLegalBasis_SourceTemplateLegalBasisId");
            entity.Property(e => e.BasisCode).HasMaxLength(100).IsUnicode(false).IsRequired();
            entity.Property(e => e.ContentVi).HasColumnType("nvarchar(max)").IsRequired();
            entity.Property(e => e.ContentEn).HasColumnType("nvarchar(max)");
            entity.Property(e => e.DisplayOrder).HasDefaultValue(0, "DF_tbl_ContractLegalBasis_DisplayOrder");
            entity.Property(e => e.CreatedDate).HasColumnType("datetime2").HasDefaultValueSql("(sysutcdatetime())", "DF_tbl_ContractLegalBasis_CreatedDate");
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime2");
            entity.Property(e => e.RowVersion).IsRowVersion().IsConcurrencyToken();
        });
    }
}
