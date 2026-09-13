using ContractManagement.Infrastructure.Persistence.Application.Models;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Infrastructure.Persistence.Application;

public partial class DbDtctechContext
{
    private static void ConfigureContractVersionSnapshots(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TblContractVersionLegalSnapshot>(entity =>
        {
            entity.ToTable("tbl_ContractVersionLegalSnapshot", table =>
            {
                table.HasCheckConstraint("CK_tbl_ContractVersionLegalSnapshot_Ids",
                    "[VersionId] > 0 AND [ContractId] > 0 AND [VersionNo] > 0 AND [TemplateVersionId] > 0 AND [CreatedByEmployeeId] > 0");
                table.HasCheckConstraint("CK_tbl_ContractVersionLegalSnapshot_Financials",
                    "[Subtotal] >= 0 AND [TotalDiscount] >= 0 AND [TotalVat] >= 0 AND [TotalAmount] >= 0");
            });
            entity.HasKey(x => x.ContractVersionLegalSnapshotId);
            entity.HasIndex(x => x.VersionId).IsUnique();
            entity.Property(x => x.ContractCode).HasMaxLength(50).IsUnicode(false);
            entity.Property(x => x.ContractName).HasMaxLength(1000);
            entity.Property(x => x.ContractNameEn).HasMaxLength(500).IsUnicode(false);
            entity.Property(x => x.CurrencyCode).HasMaxLength(3).IsFixedLength().IsUnicode(false);
            entity.Property(x => x.Subtotal).HasPrecision(18, 2);
            entity.Property(x => x.TotalDiscount).HasPrecision(18, 2);
            entity.Property(x => x.TotalVat).HasPrecision(18, 2);
            entity.Property(x => x.TotalAmount).HasPrecision(18, 2);
            entity.Property(x => x.ContractCreatedDate).HasColumnType("datetime2");
            entity.Property(x => x.SignDate).HasColumnType("datetime2");
            entity.Property(x => x.EffectiveDate).HasColumnType("datetime2");
            entity.Property(x => x.ExpireDate).HasColumnType("datetime2");
            entity.Property(x => x.CreatedDate).HasColumnType("datetime2");
            entity.HasOne(x => x.Version).WithOne(x => x.LegalSnapshot)
                .HasForeignKey<TblContractVersionLegalSnapshot>(x => x.VersionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TblContractVersionPartySnapshot>(entity =>
        {
            entity.ToTable("tbl_ContractVersionPartySnapshot", table =>
            {
                table.HasCheckConstraint("CK_tbl_ContractVersionPartySnapshot_Role", "[PartyRole] IN (1, 2)");
                table.HasCheckConstraint("CK_tbl_ContractVersionPartySnapshot_Source",
                    "([PartyRole] = 1 AND [SourceCustomerId] IS NULL) OR ([PartyRole] = 2 AND [SourceCustomerId] > 0)");
            });
            entity.HasKey(x => x.ContractVersionPartySnapshotId);
            entity.HasIndex(x => new { x.ContractVersionLegalSnapshotId, x.PartyRole }).IsUnique();
            entity.Property(x => x.LegalName).HasMaxLength(1000);
            entity.Property(x => x.TaxCode).HasMaxLength(50).IsUnicode(false);
            entity.Property(x => x.Address).HasMaxLength(2000);
            entity.Property(x => x.RepresentativeName).HasMaxLength(500);
            entity.Property(x => x.RepresentativeTitle).HasMaxLength(500);
            entity.Property(x => x.PhoneNumber).HasMaxLength(50).IsUnicode(false);
            entity.Property(x => x.FaxNumber).HasMaxLength(50).IsUnicode(false);
            entity.Property(x => x.BankAccountNumber).HasMaxLength(100).IsUnicode(false);
            entity.Property(x => x.BankName).HasMaxLength(500);
            entity.HasOne(x => x.LegalSnapshot).WithMany(x => x.Parties)
                .HasForeignKey(x => x.ContractVersionLegalSnapshotId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TblContractVersionPaymentMilestoneSnapshot>(entity =>
        {
            entity.ToTable("tbl_ContractVersionPaymentMilestoneSnapshot", table =>
            {
                table.HasCheckConstraint("CK_tbl_ContractVersionPaymentMilestoneSnapshot_Ids",
                    "[SourcePaymentMilestoneId] > 0 AND [SourceTermId] > 0 AND ([SourceTemplatePaymentMilestoneId] IS NULL OR [SourceTemplatePaymentMilestoneId] > 0)");
                table.HasCheckConstraint("CK_tbl_ContractVersionPaymentMilestoneSnapshot_Values",
                    "[PaymentPercent] > 0 AND [PaymentPercent] <= 100 AND [Amount] >= 0 AND [DisplayOrder] > 0");
            });
            entity.HasKey(x => x.ContractVersionPaymentMilestoneSnapshotId);
            entity.HasIndex(x => new { x.ContractVersionLegalSnapshotId, x.SourcePaymentMilestoneId }).IsUnique();
            entity.HasIndex(x => new { x.ContractVersionLegalSnapshotId, x.DisplayOrder });
            entity.Property(x => x.MilestoneCode).HasMaxLength(100).IsUnicode(false);
            entity.Property(x => x.TitleVi).HasMaxLength(500);
            entity.Property(x => x.TitleEn).HasMaxLength(500).IsUnicode(false);
            entity.Property(x => x.PaymentPercent).HasPrecision(9, 4);
            entity.Property(x => x.ConditionVi).HasMaxLength(2000);
            entity.Property(x => x.ConditionEn).HasMaxLength(2000).IsUnicode(false);
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.AnchorDate).HasColumnType("datetime2");
            entity.Property(x => x.DueDate).HasColumnType("datetime2");
            entity.Property(x => x.PaidAt).HasColumnType("datetime2");
            entity.HasOne(x => x.LegalSnapshot).WithMany(x => x.PaymentMilestones)
                .HasForeignKey(x => x.ContractVersionLegalSnapshotId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
