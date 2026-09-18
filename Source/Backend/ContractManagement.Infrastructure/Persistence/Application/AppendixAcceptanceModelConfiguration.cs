using ContractManagement.Infrastructure.Persistence.Application.Models;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Infrastructure.Persistence.Application;

internal static class AppendixAcceptanceModelConfiguration
{
    internal static void Configure(ModelBuilder modelBuilder)
    {
        ConfigureTemplateAppendix(modelBuilder);
        ConfigureContractAppendix(modelBuilder);
        ConfigureAcceptance(modelBuilder);
    }

    private static void ConfigureTemplateAppendix(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TblContractTemplateAppendix>(entity =>
        {
            entity.HasKey(x => x.TemplateAppendixId);
            entity.ToTable("tbl_ContractTemplateAppendix", table =>
            {
                table.HasCheckConstraint("CK_tbl_ContractTemplateAppendix_Code", "LEN(LTRIM(RTRIM([AppendixCode]))) > 0");
                table.HasCheckConstraint("CK_tbl_ContractTemplateAppendix_Name", "LEN(LTRIM(RTRIM([AppendixName]))) > 0");
                table.HasCheckConstraint("CK_tbl_ContractTemplateAppendix_Order", "[DisplayOrder] >= 0");
                table.HasCheckConstraint("CK_tbl_ContractTemplateAppendix_RequiredDefault", "[IsRequired] = 0 OR [IsSelectedByDefault] = 1");
            });
            entity.HasIndex(x => new { x.TemplateVersionId, x.AppendixCode }).IsUnique();
            entity.HasIndex(x => new { x.TemplateVersionId, x.DisplayOrder }).IsUnique();
            entity.Property(x => x.AppendixCode).HasMaxLength(50).IsUnicode(false);
            entity.Property(x => x.AppendixName).HasMaxLength(500);
            entity.Property(x => x.AppendixNameEn).HasMaxLength(500).IsUnicode();
            entity.Property(x => x.AppendixDescription).HasMaxLength(2000);
            entity.Property(x => x.CreatedDate).HasColumnType("datetime2").HasDefaultValueSql("sysutcdatetime()");
            entity.Property(x => x.UpdatedDate).HasColumnType("datetime2");
            entity.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
            entity.HasOne<TblContractTemplateVersion>().WithMany().HasForeignKey(x => x.TemplateVersionId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<TblEmployee>().WithMany().HasForeignKey(x => x.CreatedEmployeeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<TblEmployee>().WithMany().HasForeignKey(x => x.UpdatedEmployeeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TblContractTemplateAppendixTerm>(entity =>
        {
            entity.HasKey(x => x.TemplateAppendixTermId);
            entity.ToTable("tbl_ContractTemplateAppendixTerm", table =>
            {
                table.HasCheckConstraint("CK_tbl_ContractTemplateAppendixTerm_Code", "LEN(LTRIM(RTRIM([TermCode]))) > 0");
                table.HasCheckConstraint("CK_tbl_ContractTemplateAppendixTerm_Title", "LEN(LTRIM(RTRIM([TermTitle]))) > 0");
                table.HasCheckConstraint("CK_tbl_ContractTemplateAppendixTerm_Order", "[DisplayOrder] >= 0");
            });
            entity.HasIndex(x => new { x.TemplateAppendixId, x.TermCode }).IsUnique();
            entity.HasIndex(x => new { x.TemplateAppendixId, x.DisplayOrder }).IsUnique();
            entity.Property(x => x.TermCode).HasMaxLength(100).IsUnicode(false);
            entity.Property(x => x.TermTitle).HasMaxLength(1000);
            entity.Property(x => x.TermTitleEn).HasMaxLength(1000);
            entity.Property(x => x.TermContent).HasColumnType("nvarchar(max)");
            entity.Property(x => x.TermContentEn).HasColumnType("nvarchar(max)");
            entity.Property(x => x.CreatedDate).HasColumnType("datetime2").HasDefaultValueSql("sysutcdatetime()");
            entity.Property(x => x.UpdatedDate).HasColumnType("datetime2");
            entity.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
            entity.HasOne<TblContractTemplateAppendix>().WithMany().HasForeignKey(x => x.TemplateAppendixId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<TblEmployee>().WithMany().HasForeignKey(x => x.CreatedEmployeeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<TblEmployee>().WithMany().HasForeignKey(x => x.UpdatedEmployeeId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureContractAppendix(ModelBuilder modelBuilder)
    {
        var appendix = modelBuilder.Entity<TblContractAppendix>();
        appendix.HasKey(x => x.AppendixId);
        appendix.ToTable("tbl_ContractAppendix", table =>
        {
            table.HasCheckConstraint("CK_tbl_ContractAppendix_Order", "[DisplayOrder] >= 0");
            table.HasCheckConstraint("CK_tbl_ContractAppendix_Code", "LEN(LTRIM(RTRIM([AppendixCode]))) > 0");
        });
        appendix.HasIndex(x => new { x.VersionId, x.AppendixCode }).IsUnique();
        appendix.HasIndex(x => new { x.VersionId, x.DisplayOrder }).IsUnique();
        appendix.HasIndex(x => new { x.ContractId, x.VersionId, x.DisplayOrder });
        appendix.Property(x => x.AppendixCode).HasMaxLength(50).IsUnicode(false);
        appendix.Property(x => x.AppendixName).HasMaxLength(500);
        appendix.Property(x => x.AppendixNameEn).HasMaxLength(500).IsUnicode();
        appendix.Property(x => x.AppendixDescription).HasMaxLength(2000);
        appendix.Property(x => x.CreatedDate).HasColumnType("datetime2").HasDefaultValueSql("sysutcdatetime()");
        appendix.Property(x => x.UpdatedDate).HasColumnType("datetime2");
        appendix.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        appendix.HasOne<TblContract>().WithMany().HasForeignKey(x => x.ContractId).OnDelete(DeleteBehavior.Restrict);
        appendix.HasOne<TblContractVersion>().WithMany().HasForeignKey(x => x.VersionId).OnDelete(DeleteBehavior.Restrict);
        appendix.HasOne<TblContractTemplateAppendix>().WithMany().HasForeignKey(x => x.SourceTemplateAppendixId).OnDelete(DeleteBehavior.Restrict);
        appendix.HasOne<TblEmployee>().WithMany().HasForeignKey(x => x.CreatedEmployeeId).OnDelete(DeleteBehavior.Restrict);
        appendix.HasOne<TblEmployee>().WithMany().HasForeignKey(x => x.UpdatedEmployeeId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TblContractAppendixTerm>(entity =>
        {
            entity.HasKey(x => x.AppendixTermId);
            entity.ToTable("tbl_ContractAppendixTerm", table => table.HasCheckConstraint("CK_tbl_ContractAppendixTerm_Order", "[DisplayOrder] >= 0"));
            entity.HasIndex(x => new { x.AppendixId, x.TermCode }).IsUnique();
            entity.HasIndex(x => new { x.AppendixId, x.DisplayOrder }).IsUnique();
            entity.Property(x => x.TermCode).HasMaxLength(100).IsUnicode(false);
            entity.Property(x => x.TermTitle).HasMaxLength(1000);
            entity.Property(x => x.TermTitleEn).HasMaxLength(1000);
            entity.Property(x => x.TermContent).HasColumnType("nvarchar(max)");
            entity.Property(x => x.TermContentEn).HasColumnType("nvarchar(max)");
            entity.Property(x => x.CreatedDate).HasColumnType("datetime2").HasDefaultValueSql("sysutcdatetime()");
            entity.Property(x => x.UpdatedDate).HasColumnType("datetime2");
            entity.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
            entity.HasOne<TblContractAppendix>().WithMany().HasForeignKey(x => x.AppendixId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<TblContractTemplateAppendixTerm>().WithMany().HasForeignKey(x => x.SourceTemplateAppendixTermId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureAcceptance(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TblContractAcceptanceRecord>(entity =>
        {
            entity.HasKey(x => x.AcceptanceRecordId);
            entity.ToTable("tbl_ContractAcceptanceRecord", table =>
            {
                table.HasCheckConstraint("CK_tbl_ContractAcceptanceRecord_Kind", "[AcceptanceKind] IN (1, 2)");
                table.HasCheckConstraint("CK_tbl_ContractAcceptanceRecord_Status", "[Status] IN (0, 1, 2, 3)");
                table.HasCheckConstraint("CK_tbl_ContractAcceptanceRecord_Hash", "[SnapshotHash] IS NULL OR LEN([SnapshotHash]) = 64");
            });
            entity.HasIndex(x => new { x.ContractId, x.AcceptanceCode }).IsUnique();
            entity.HasIndex(x => new { x.ContractVersionId, x.Status });
            entity.Property(x => x.AcceptanceCode).HasMaxLength(100).IsUnicode(false);
            entity.Property(x => x.AcceptanceDate).HasColumnType("date");
            entity.Property(x => x.Location).HasMaxLength(500);
            entity.Property(x => x.SnapshotHash).HasMaxLength(64).IsUnicode(false).IsFixedLength();
            entity.Property(x => x.CancellationReason).HasMaxLength(1000);
            entity.Property(x => x.FinalizedAt).HasColumnType("datetime2");
            entity.Property(x => x.SignedAt).HasColumnType("datetime2");
            entity.Property(x => x.CancelledAt).HasColumnType("datetime2");
            entity.Property(x => x.CreatedDate).HasColumnType("datetime2").HasDefaultValueSql("sysutcdatetime()");
            entity.Property(x => x.UpdatedDate).HasColumnType("datetime2");
            entity.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
            entity.HasOne<TblContract>().WithMany().HasForeignKey(x => x.ContractId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<TblContractVersion>().WithMany().HasForeignKey(x => x.ContractVersionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<TblContractTemplateVersion>().WithMany().HasForeignKey(x => x.TemplateVersionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<TblEmployee>().WithMany().HasForeignKey(x => x.CreatedEmployeeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<TblEmployee>().WithMany().HasForeignKey(x => x.UpdatedEmployeeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<TblEmployee>().WithMany().HasForeignKey(x => x.FinalizedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<TblEmployee>().WithMany().HasForeignKey(x => x.SignedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<TblEmployee>().WithMany().HasForeignKey(x => x.CancelledByEmployeeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TblContractAcceptanceReference>(entity =>
        {
            entity.HasKey(x => x.AcceptanceReferenceId);
            entity.ToTable("tbl_ContractAcceptanceReference", table =>
            {
                table.HasCheckConstraint("CK_tbl_ContractAcceptanceReference_Type", "[ReferenceType] IN (1, 2)");
                table.HasCheckConstraint("CK_tbl_ContractAcceptanceReference_Target", "([ReferenceType] = 1 AND [AppendixId] IS NULL) OR ([ReferenceType] = 2 AND [AppendixId] IS NOT NULL)");
            });
            entity.HasIndex(x => new { x.AcceptanceRecordId, x.DisplayOrder }).IsUnique();
            entity.HasIndex(x => new { x.AcceptanceRecordId, x.ReferenceType, x.AppendixId }).IsUnique();
            entity.HasOne<TblContractAcceptanceRecord>().WithMany().HasForeignKey(x => x.AcceptanceRecordId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<TblContractAppendix>().WithMany().HasForeignKey(x => x.AppendixId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TblContractAcceptanceParty>(entity =>
        {
            entity.HasKey(x => x.AcceptancePartyId);
            entity.ToTable("tbl_ContractAcceptanceParty", table => table.HasCheckConstraint("CK_tbl_ContractAcceptanceParty_Role", "[PartyRole] IN (1, 2)"));
            entity.HasIndex(x => new { x.AcceptanceRecordId, x.PartyRole }).IsUnique();
            entity.Property(x => x.LegalName).HasMaxLength(500);
            entity.Property(x => x.Address).HasMaxLength(1000);
            entity.Property(x => x.Phone).HasMaxLength(50);
            entity.Property(x => x.Fax).HasMaxLength(50);
            entity.Property(x => x.TaxCode).HasMaxLength(50).IsUnicode(false);
            entity.Property(x => x.RepresentativeName).HasMaxLength(300);
            entity.Property(x => x.RepresentativeTitle).HasMaxLength(300);
            entity.HasOne<TblContractAcceptanceRecord>().WithMany().HasForeignKey(x => x.AcceptanceRecordId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TblContractAcceptanceSection>(entity =>
        {
            entity.HasKey(x => x.AcceptanceSectionId);
            entity.ToTable("tbl_ContractAcceptanceSection", table => table.HasCheckConstraint("CK_tbl_ContractAcceptanceSection_Order", "[DisplayOrder] >= 0"));
            entity.HasIndex(x => new { x.AcceptanceRecordId, x.SectionCode }).IsUnique();
            entity.HasIndex(x => new { x.AcceptanceRecordId, x.DisplayOrder }).IsUnique();
            entity.Property(x => x.SectionCode).HasMaxLength(100).IsUnicode(false);
            entity.Property(x => x.TitleVi).HasMaxLength(1000);
            entity.Property(x => x.TitleEn).HasMaxLength(1000);
            entity.Property(x => x.ContentVi).HasColumnType("nvarchar(max)");
            entity.Property(x => x.ContentEn).HasColumnType("nvarchar(max)");
            entity.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
            entity.HasOne<TblContractAcceptanceRecord>().WithMany().HasForeignKey(x => x.AcceptanceRecordId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TblContractAcceptanceMilestone>(entity =>
        {
            entity.HasKey(x => x.AcceptanceMilestoneId);
            entity.ToTable("tbl_ContractAcceptanceMilestone");
            entity.HasIndex(x => new { x.AcceptanceRecordId, x.PaymentMilestoneId }).IsUnique();
            entity.HasOne<TblContractAcceptanceRecord>().WithMany().HasForeignKey(x => x.AcceptanceRecordId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<TblContractPaymentMilestone>().WithMany().HasForeignKey(x => x.PaymentMilestoneId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TblContractAcceptanceEvidence>(entity =>
        {
            entity.HasIndex(x => x.AcceptanceRecordId).IsUnique();
            entity.HasOne<TblContractAcceptanceRecord>().WithMany().HasForeignKey(x => x.AcceptanceRecordId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
