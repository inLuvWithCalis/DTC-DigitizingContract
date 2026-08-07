using System;
using System.Collections.Generic;
using ContractManagement.Infrastructure.Persistence.Application.Models;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Infrastructure.Persistence.Application;

public partial class DbDtctechContext : DbContext
{
    public DbDtctechContext()
    {
    }

    public DbDtctechContext(DbContextOptions<DbDtctechContext> options)
        : base(options)
    {
    }

    public virtual DbSet<TblContractApprovalRequest> TblContractApprovalRequests { get; set; }

    public virtual DbSet<TblApprovalHistory> TblApprovalHistories { get; set; }

    public virtual DbSet<TblApprovalWorkflow> TblApprovalWorkflows { get; set; }

    public virtual DbSet<TblCategory> TblCategories { get; set; }

    public virtual DbSet<TblContract> TblContracts { get; set; }

    public virtual DbSet<TblContractAudit> TblContractAudits { get; set; }

    public virtual DbSet<TblContractCustomerVerificationPhone>
        TblContractCustomerVerificationPhones { get; set; }

    public virtual DbSet<TblContractCustomerAccessLink>
        TblContractCustomerAccessLinks { get; set; }

    public virtual DbSet<TblContractCustomerOtpChallenge>
        TblContractCustomerOtpChallenges { get; set; }

    public virtual DbSet<TblContractCustomerAccessSession>
        TblContractCustomerAccessSessions { get; set; }

    public virtual DbSet<TblContractCustomerOtpDeliveryOutbox>
        TblContractCustomerOtpDeliveryOutbox { get; set; }

    public virtual DbSet<TblContractNegotiationComment>
        TblContractNegotiationComments { get; set; }

    public virtual DbSet<TblContractNegotiationCommentEvent>
        TblContractNegotiationCommentEvents { get; set; }

    public virtual DbSet<TblContractAppendix> TblContractAppendices { get; set; }

    public virtual DbSet<TblContractAttachment> TblContractAttachments { get; set; }

    public virtual DbSet<TblContractItem> TblContractItems { get; set; }

    public virtual DbSet<TblContractTerm> TblContractTerms { get; set; }

    public virtual DbSet<TblContractVersion> TblContractVersions { get; set; }

    /*
     * Template và TemplateVersion là hai bảng quan trọng nhất.
     * TemplateVersion là snapshot bất biến của template.
     * Template có thể có nhiều version, nhưng chỉ có một version được publish.
     */
    public virtual DbSet<TblContractTemplate> TblContractTemplates { get; set; }

    public virtual DbSet<TblContractTemplateVersion> TblContractTemplateVersions { get; set; }


    /*
     * TemplateField và TemplateTerm là hai bảng cấu hình cho template version.
     * Khi tạo contract version, dữ liệu sẽ được sao chép sang ContractTerm.
     */
    public virtual DbSet<TblContractTemplateField> TblContractTemplateFields { get; set; }

    public virtual DbSet<TblContractTemplateTerm> TblContractTemplateTerms { get; set; }

    public virtual DbSet<TblCustomer> TblCustomers { get; set; }

    public virtual DbSet<TblCustomerInteraction> TblCustomerInteractions { get; set; }

    public virtual DbSet<TblDeliveryDetail> TblDeliveryDetails { get; set; }

    public virtual DbSet<TblDeliveryOrder> TblDeliveryOrders { get; set; }

    public virtual DbSet<TblDepartment> TblDepartments { get; set; }

    public virtual DbSet<TblEmployee> TblEmployees { get; set; }

    public virtual DbSet<TblFileStorage> TblFileStorages { get; set; }

    public virtual DbSet<TblInvoice> TblInvoices { get; set; }

    public virtual DbSet<TblNotification> TblNotifications { get; set; }

    public virtual DbSet<TblOrder> TblOrders { get; set; }

    public virtual DbSet<TblOrderDetail> TblOrderDetails { get; set; }

    public virtual DbSet<TblPayment> TblPayments { get; set; }

    public virtual DbSet<TblPaymentSchedule> TblPaymentSchedules { get; set; }

    public virtual DbSet<TblProduct> TblProducts { get; set; }

    public virtual DbSet<TblQuotation> TblQuotations { get; set; }

    public virtual DbSet<TblQuotationDetail> TblQuotationDetails { get; set; }

    public virtual DbSet<TblService> TblServices { get; set; }

    public virtual DbSet<TblServiceType> TblServiceTypes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TblContractApprovalRequest>(entity =>
        {
            entity.HasKey(x => x.ApprovalRequestId);

            entity.ToTable("tbl_ContractApprovalRequest", table =>
            {
                table.HasCheckConstraint(
                    "CK_tbl_ContractApprovalRequest_Status",
                    "[Status] IN (0, 1, 2, 3, 4)");

                table.HasCheckConstraint(
                    "CK_tbl_ContractApprovalRequest_ContractId",
                    "[ContractId] > 0");

                table.HasCheckConstraint(
                    "CK_tbl_ContractApprovalRequest_VersionId",
                    "[VersionId] > 0");
            });

            /*
             * Mỗi hợp đồng chỉ được có một request Pending.
             * Các request cũ đã kết thúc vẫn được giữ làm lịch sử.
             */
            entity.HasIndex(x => x.ContractId)
                .IsUnique()
                .HasFilter("[Status] = 0")
                .HasDatabaseName(
                    "UX_tbl_ContractApprovalRequest_PendingContract");

            entity.HasIndex(x => x.VersionId)
                .HasDatabaseName(
                    "IX_tbl_ContractApprovalRequest_VersionId");

            entity.HasIndex(x => x.WorkflowId)
                .HasDatabaseName(
                    "IX_tbl_ContractApprovalRequest_WorkflowId");

            entity.Property(x => x.Status)
                .HasDefaultValue((byte)0);

            entity.Property(x => x.SubmittedDate)
                .HasColumnType("datetime2")
                .HasDefaultValueSql("(sysutcdatetime())");

            entity.Property(x => x.ResolvedDate)
                .HasColumnType("datetime2");

            entity.Property(x => x.DecisionComment)
                .HasMaxLength(1000);

            entity.Property(x => x.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
        });

        modelBuilder.Entity<TblApprovalHistory>(entity =>
        {
            entity.HasKey(e => e.ApprovalHistoryId).HasName("PK__tbl_Appr__46B53247FB4F4CF5");

            entity.ToTable("tbl_ApprovalHistory");

            entity.Property(e => e.ActionDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ApprovalAction)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Comment).HasMaxLength(1000);
            entity.Property(e => e.ObjectType)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TblApprovalWorkflow>(entity =>
        {
            entity.HasKey(e => e.WorkflowId).HasName("PK__tbl_Appr__5704A66A2F3CD2A5");

            entity.ToTable("tbl_ApprovalWorkflow");

            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.ObjectType)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.WorkflowName).HasMaxLength(200);
        });

        modelBuilder.Entity<TblCategory>(entity =>
        {
            entity.HasKey(e => e.CategoryId);

            entity.ToTable("tbl_Categories");

            entity.Property(e => e.CategoryId).ValueGeneratedOnAdd();
            entity.Property(e => e.CategoryName).HasMaxLength(500);
            entity.Property(e => e.CategoryShortDesc).HasMaxLength(1000);
            entity.Property(e => e.Image)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TblContract>(entity =>
        {
            entity.HasKey(e => e.ContractId);

            entity.ToTable("tbl_Contract", table =>
            {
                // Chỉ chấp nhận các trạng thái thuộc lifecycle.
                table.HasCheckConstraint(
                    "CK_tbl_Contract_Status",
                    "[Status] IN (0, 1, 2, 3, 4, 5, 6, 7)");

                // ContractType hiện có ba loại hợp đồng hợp lệ.
                // Không cho phép giá trị 0 vì 0 không mang ý nghĩa nghiệp vụ.
                table.HasCheckConstraint(
                    "CK_tbl_Contract_ContractType",
                    "[ContractType] IN (1, 2, 3)");

                // 1 = Vietnamese, 2 = Bilingual.
                table.HasCheckConstraint(
                    "CK_tbl_Contract_LanguageMode",
                    "[LanguageMode] IN (1, 2)");

                // Giá trị hợp đồng không được âm.
                table.HasCheckConstraint(
                    "CK_tbl_Contract_TotalAmount",
                    "[TotalAmount] >= 0");

                table.HasCheckConstraint(
                    "CK_tbl_Contract_FinancialTotals",
                    "[Subtotal] >= 0 AND [TotalDiscount] >= 0 " +
                    "AND [TotalVat] >= 0 AND [TotalAmount] >= 0");
            });
            /*
             * ContractCode có thể null khi còn Draft.
             * Khi đã có code thì code phải duy nhất trong tenant.
             */
            entity.HasIndex(
                    e => e.ContractCode,
                    "UX_tbl_Contract_ContractCode")
                .IsUnique()
                .HasFilter("[ContractCode] IS NOT NULL");

            /*
             * Các index phục vụ query theo Customer, Owner và trạng thái.
             * Dự án không dùng foreign key vật lý nhưng vẫn cần index.
             */
            entity.HasIndex(
                e => e.CustomerId,
                "IX_tbl_Contract_CustomerId");

            entity.HasIndex(
                e => new { e.EmployeeId, e.Status },
                "IX_tbl_Contract_EmployeeId_Status");

            entity.HasIndex(
                e => e.TemplateVersionId,
                "IX_tbl_Contract_TemplateVersionId");

            entity.HasIndex(
                e => e.ParentContractId,
                "IX_tbl_Contract_ParentContractId");

            entity.HasIndex(
                e => e.CurrentVersionId,
                "IX_tbl_Contract_CurrentVersionId");

            entity.HasIndex(e => e.CurrentVerificationPhoneId)
                .HasDatabaseName("IX_tbl_Contract_CurrentVerificationPhoneId");

            entity.HasIndex(e => e.CurrentCustomerAccessLinkId)
                .HasDatabaseName("IX_tbl_Contract_CurrentCustomerAccessLinkId");

            entity.Property(e => e.ContractCode)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.Property(e => e.ContractName)
                .HasMaxLength(1000)
                .IsRequired();

            entity.Property(e => e.ContractNameEn)
                .HasMaxLength(1000);

            entity.Property(e => e.SignDate)
                .HasColumnType("datetime2");

            entity.Property(e => e.EffectiveDate)
                .HasColumnType("datetime2");

            entity.Property(e => e.ExpireDate)
                .HasColumnType("datetime2");

            /*
             * ContractStatus.Draft = 0.
             * Không reference API enum từ Infrastructure.
             */
            entity.Property(e => e.Status)
                .HasDefaultValue((byte)0);

            /*
             * Tiền bắt buộc dùng decimal.
             */
            entity.Property(e => e.TotalAmount)
                .HasPrecision(18, 2)
                .HasDefaultValue(0m);

            entity.Property(e => e.Subtotal)
                .HasPrecision(18, 2)
                .HasDefaultValue(0m);

            entity.Property(e => e.TotalDiscount)
                .HasPrecision(18, 2)
                .HasDefaultValue(0m);

            entity.Property(e => e.TotalVat)
                .HasPrecision(18, 2)
                .HasDefaultValue(0m);

            entity.Property(e => e.CurrencyCode)
                .HasMaxLength(3)
                .IsUnicode(false)
                .IsFixedLength()
                .HasDefaultValue("VND");

            /*
             * ContractLanguageMode.Vietnamese = 1.
             */
            entity.Property(e => e.LanguageMode)
                .HasDefaultValue((byte)1);

            entity.Property(e => e.IsLegacy)
                .HasDefaultValue(false);

            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql(
                    "(sysutcdatetime())",
                    "DF_tbl_Contract_CreatedDate")
                .HasColumnType("datetime2");

            entity.Property(e => e.UpdateDate)
                .HasColumnType("datetime2");

            /*
             * RowVersion do SQL Server tự sinh.
             * Client không được tự gán giá trị cho cột này.
             */
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
        });

        modelBuilder.Entity<TblContractAudit>(entity =>
        {
            entity.HasKey(e => e.ContractAuditId)
                .HasName("PK_tbl_ContractAudit");

            entity.ToTable("tbl_ContractAudit", table =>
            {
                table.HasTrigger(
                    "TR_tbl_ContractAudit_AppendOnly");

                table.HasCheckConstraint(
                    "CK_tbl_ContractAudit_TenantId",
                    "[TenantId] > 0");

                table.HasCheckConstraint(
                    "CK_tbl_ContractAudit_ContractId",
                    "[ContractId] > 0");

                table.HasCheckConstraint(
                    "CK_tbl_ContractAudit_VersionId",
                    "[VersionId] IS NULL OR [VersionId] > 0");

                table.HasCheckConstraint(
                    "CK_tbl_ContractAudit_Subject",
                    "([SubjectType] IS NULL AND [SubjectId] IS NULL) OR " +
                    "([SubjectType] IN ('Contract', 'ContractVersion', " +
                    "'NegotiationComment', 'CustomerAccessLink', " +
                    "'CustomerOtpChallenge', 'CustomerAccessSession') " +
                    "AND [SubjectId] > 0)");

                table.HasCheckConstraint(
                    "CK_tbl_ContractAudit_ActorType",
                    "LEN(LTRIM(RTRIM([ActorType]))) > 0");

                table.HasCheckConstraint(
                    "CK_tbl_ContractAudit_Actor",
                    "([ActorType] = 'Employee' " +
                    "AND [ActorEmployeeId] > 0 " +
                    "AND [ActorCustomerAccessSessionId] IS NULL) OR " +
                    "([ActorType] = 'Customer' " +
                    "AND [ActorEmployeeId] IS NULL " +
                    "AND [ActorCustomerAccessSessionId] > 0) OR " +
                    "([ActorType] = 'System' " +
                    "AND [ActorEmployeeId] IS NULL " +
                    "AND [ActorCustomerAccessSessionId] IS NULL)");

                table.HasCheckConstraint(
                    "CK_tbl_ContractAudit_ActionType",
                    "LEN(LTRIM(RTRIM([ActionType]))) > 0");

                table.HasCheckConstraint(
                    "CK_tbl_ContractAudit_Result",
                    "LEN(LTRIM(RTRIM([Result]))) > 0");

                table.HasCheckConstraint(
                    "CK_tbl_ContractAudit_PreviousResponsibleEmployeeId",
                    "[PreviousResponsibleEmployeeId] IS NULL " +
                    "OR [PreviousResponsibleEmployeeId] > 0");

                table.HasCheckConstraint(
                    "CK_tbl_ContractAudit_NewResponsibleEmployeeId",
                    "[NewResponsibleEmployeeId] IS NULL " +
                    "OR [NewResponsibleEmployeeId] > 0");

                table.HasCheckConstraint(
                    "CK_tbl_ContractAudit_CorrelationId",
                    "LEN(LTRIM(RTRIM([CorrelationId]))) > 0");

                table.HasCheckConstraint(
                    "CK_tbl_ContractAudit_PreviousValuesJson",
                    "[PreviousValuesJson] IS NULL OR " +
                    "(ISJSON([PreviousValuesJson]) = 1 AND " +
                    "LEFT(LTRIM([PreviousValuesJson]), 1) = '{')");

                table.HasCheckConstraint(
                    "CK_tbl_ContractAudit_NewValuesJson",
                    "[NewValuesJson] IS NULL OR " +
                    "(ISJSON([NewValuesJson]) = 1 AND " +
                    "LEFT(LTRIM([NewValuesJson]), 1) = '{')");

                table.HasCheckConstraint(
                    "CK_tbl_ContractAudit_FailureCode",
                    "[FailureCode] IS NULL OR " +
                    "LEN(LTRIM(RTRIM([FailureCode]))) > 0");
            });

            entity.HasIndex(e => new
                {
                    e.TenantId,
                    e.ContractId,
                    e.OccurredAt
                })
                .HasDatabaseName(
                    "IX_tbl_ContractAudit_TenantId_ContractId_OccurredAt");

            entity.HasIndex(e => new
                {
                    e.TenantId,
                    e.OccurredAt,
                    e.ContractAuditId
                })
                .IsDescending(false, true, true)
                .HasDatabaseName(
                    "IX_tbl_ContractAudit_TenantId_OccurredAt_ContractAuditId");

            entity.Property(e => e.SubjectType)
                .HasMaxLength(64)
                .IsUnicode(false);

            entity.Property(e => e.SubjectId);

            entity.Property(e => e.ActorType)
                .HasMaxLength(32)
                .IsUnicode(false);

            entity.Property(e => e.ActionType)
                .HasMaxLength(64)
                .IsUnicode(false);

            entity.Property(e => e.ActorCustomerAccessSessionId);

            entity.Property(e => e.Result)
                .HasMaxLength(32)
                .IsUnicode(false);

            entity.Property(e => e.Reason)
                .HasMaxLength(1000);

            entity.Property(e => e.PreviousValuesJson)
                .HasColumnType("nvarchar(max)");

            entity.Property(e => e.NewValuesJson)
                .HasColumnType("nvarchar(max)");

            entity.Property(e => e.FailureCode)
                .HasMaxLength(64)
                .IsUnicode(false);

            entity.Property(e => e.OccurredAt)
                .HasColumnType("datetime2");

            entity.Property(e => e.IpAddress)
                .HasMaxLength(45)
                .IsUnicode(false);

            entity.Property(e => e.UserAgent)
                .HasMaxLength(1024);

            entity.Property(e => e.CorrelationId)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TblContractNegotiationComment>(entity =>
        {
            entity.HasKey(e => e.CommentId)
                .HasName("PK_tbl_ContractNegotiationComment");

            entity.ToTable("tbl_ContractNegotiationComment", table =>
            {
                table.HasTrigger(
                    "TR_tbl_ContractNegotiationComment_AppendOnly");

                table.HasCheckConstraint(
                    "CK_tbl_ContractNegotiationComment_ContractId",
                    "[ContractId] > 0");

                table.HasCheckConstraint(
                    "CK_tbl_ContractNegotiationComment_VersionId",
                    "[VersionId] > 0");

                table.HasCheckConstraint(
                    "CK_tbl_ContractNegotiationComment_TermId",
                    "[TermId] IS NULL OR [TermId] > 0");

                table.HasCheckConstraint(
                    "CK_tbl_ContractNegotiationComment_ParentCommentId",
                    "[ParentCommentId] IS NULL OR [ParentCommentId] > 0");

                table.HasCheckConstraint(
                    "CK_tbl_ContractNegotiationComment_Content",
                    "LEN(LTRIM(RTRIM([Content]))) BETWEEN 1 AND 4000");

                table.HasCheckConstraint(
                    "CK_tbl_ContractNegotiationComment_Source",
                    "[Source] IN ('ExternalFeedback', 'Customer')");

                table.HasCheckConstraint(
                    "CK_tbl_ContractNegotiationComment_Actor",
                    "([Source] = 'ExternalFeedback' " +
                    "AND [RecordedByEmployeeId] > 0 " +
                    "AND [CustomerAccessSessionId] IS NULL) OR " +
                    "([Source] = 'Customer' " +
                    "AND [RecordedByEmployeeId] IS NULL " +
                    "AND [CustomerAccessSessionId] > 0)");

                table.HasCheckConstraint(
                    "CK_tbl_ContractNegotiationComment_State",
                    "[State] IN (0, 1)");
            });

            entity.HasIndex(e => new
            {
                e.ContractId,
                e.VersionId,
                e.CreatedDate,
                e.CommentId
            })
                .HasDatabaseName(
                    "IX_tbl_ContractNegotiationComment_Version_Chronological");

            entity.HasIndex(e => e.ParentCommentId)
                .HasDatabaseName(
                    "IX_tbl_ContractNegotiationComment_ParentCommentId");

            entity.HasIndex(e => e.TermId)
                .HasDatabaseName("IX_tbl_ContractNegotiationComment_TermId");

            entity.Property(e => e.Content)
                .HasColumnType("nvarchar(4000)")
                .IsRequired();

            entity.Property(e => e.Source)
                .HasMaxLength(32)
                .IsUnicode(false)
                .HasDefaultValue("ExternalFeedback");

            entity.Property(e => e.CreatedDate)
                .HasColumnType("datetime2")
                .HasDefaultValueSql(
                    "(sysutcdatetime())",
                    "DF_tbl_ContractNegotiationComment_CreatedDate");

            entity.Property(e => e.UpdatedDate)
                .HasColumnType("datetime2");

            entity.Property(e => e.State)
                .HasDefaultValue((byte)0);

            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
        });

        modelBuilder.Entity<TblContractNegotiationCommentEvent>(entity =>
        {
            entity.HasKey(e => e.CommentEventId)
                .HasName("PK_tbl_ContractNegotiationCommentEvent");

            entity.ToTable("tbl_ContractNegotiationCommentEvent", table =>
            {
                table.HasTrigger(
                    "TR_tbl_ContractNegotiationCommentEvent_AppendOnly");

                table.HasCheckConstraint(
                    "CK_tbl_ContractNegotiationCommentEvent_CommentId",
                    "[CommentId] > 0");

                table.HasCheckConstraint(
                    "CK_tbl_ContractNegotiationCommentEvent_EventType",
                    "[EventType] IN (1, 2, 3)");

                table.HasCheckConstraint(
                    "CK_tbl_ContractNegotiationCommentEvent_Actor",
                    "([ActorType] = 'Employee' AND [EmployeeId] > 0 " +
                    "AND [CustomerAccessSessionId] IS NULL) OR " +
                    "([ActorType] = 'Customer' AND [EmployeeId] IS NULL " +
                    "AND [CustomerAccessSessionId] > 0) OR " +
                    "([ActorType] = 'System' AND [EmployeeId] IS NULL " +
                    "AND [CustomerAccessSessionId] IS NULL)");
            });

            entity.HasIndex(e => new
            {
                e.CommentId,
                e.OccurredAt,
                e.CommentEventId
            })
                .HasDatabaseName(
                    "IX_tbl_ContractNegotiationCommentEvent_Chronological");

            entity.Property(e => e.OccurredAt)
                .HasColumnType("datetime2")
                .HasDefaultValueSql(
                    "(sysutcdatetime())",
                    "DF_tbl_ContractNegotiationCommentEvent_OccurredAt");

            entity.Property(e => e.ActorType)
                .HasMaxLength(32)
                .IsUnicode(false)
                .HasDefaultValue("Employee");
        });

        modelBuilder.Entity<TblContractCustomerVerificationPhone>(entity =>
        {
            entity.HasKey(e => e.VerificationPhoneId)
                .HasName("PK_tbl_ContractCustomerVerificationPhone");

            entity.ToTable("tbl_ContractCustomerVerificationPhone", table =>
            {
                table.HasCheckConstraint("CK_tbl_ContractCustomerVerificationPhone_ContractId", "[ContractId] > 0");
                table.HasCheckConstraint("CK_tbl_ContractCustomerVerificationPhone_Source", "[PhoneSource] IN ('CustomerMobile', 'CustomerPhone', 'Manual')");
                table.HasCheckConstraint("CK_tbl_ContractCustomerVerificationPhone_Phone", "LEN(LTRIM(RTRIM([PhoneNumberNormalized]))) BETWEEN 3 AND 32");
                table.HasCheckConstraint("CK_tbl_ContractCustomerVerificationPhone_Reason", "LEN(LTRIM(RTRIM([Reason]))) BETWEEN 1 AND 1000");
                table.HasCheckConstraint("CK_tbl_ContractCustomerVerificationPhone_CreatedBy", "[CreatedByEmployeeId] > 0");
            });

            entity.HasIndex(e => new { e.ContractId, e.CreatedDate, e.VerificationPhoneId })
                .HasDatabaseName("IX_tbl_ContractCustomerVerificationPhone_Contract_Chronological");
            entity.HasIndex(e => new { e.ContractId, e.PhoneNumberNormalized })
                .HasDatabaseName("IX_tbl_ContractCustomerVerificationPhone_Contract_Phone");
            entity.Property(e => e.PhoneSource).HasMaxLength(32).IsUnicode(false);
            entity.Property(e => e.PhoneNumberNormalized).HasMaxLength(32).IsUnicode(false);
            entity.Property(e => e.Reason).HasMaxLength(1000);
            entity.Property(e => e.CreatedDate).HasColumnType("datetime2").HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.RowVersion).IsRowVersion().IsConcurrencyToken();
        });

        modelBuilder.Entity<TblContractCustomerAccessLink>(entity =>
        {
            entity.HasKey(e => e.CustomerAccessLinkId)
                .HasName("PK_tbl_ContractCustomerAccessLink");

            entity.ToTable("tbl_ContractCustomerAccessLink", table =>
            {
                table.HasCheckConstraint("CK_tbl_ContractCustomerAccessLink_LogicalIds", "[TenantId] > 0 AND [ContractId] > 0 AND [VersionId] > 0 AND [VerificationPhoneId] > 0 AND [CreatedByEmployeeId] > 0");
                table.HasCheckConstraint("CK_tbl_ContractCustomerAccessLink_TokenHash", "LEN(LTRIM(RTRIM([TokenHash]))) = 64");
                table.HasCheckConstraint("CK_tbl_ContractCustomerAccessLink_Expiry", "[ExpiresAt] > [CreatedDate]");
                table.HasCheckConstraint("CK_tbl_ContractCustomerAccessLink_Activation", "[ActivatedAt] IS NULL OR ([ActivatedAt] >= [CreatedDate] AND [ActivatedAt] <= [ExpiresAt])");
                table.HasCheckConstraint("CK_tbl_ContractCustomerAccessLink_Revocation", "([RevokedAt] IS NULL AND [RevokedByEmployeeId] IS NULL AND [RevocationReason] IS NULL) OR ([RevokedAt] IS NOT NULL AND [RevokedByEmployeeId] > 0 AND LEN(LTRIM(RTRIM([RevocationReason]))) BETWEEN 1 AND 1000)");
            });

            entity.HasIndex(e => e.TokenHash).IsUnique().HasDatabaseName("UX_tbl_ContractCustomerAccessLink_TokenHash");
            entity.HasIndex(e => e.ContractId).IsUnique().HasFilter("[RevokedAt] IS NULL").HasDatabaseName("UX_tbl_ContractCustomerAccessLink_ActiveContract");
            entity.HasIndex(e => new { e.ContractId, e.VersionId, e.VerificationPhoneId }).HasFilter("[RevokedAt] IS NULL").HasDatabaseName("IX_tbl_ContractCustomerAccessLink_ActiveContext");
            entity.HasIndex(e => new { e.ExpiresAt, e.RevokedAt }).HasDatabaseName("IX_tbl_ContractCustomerAccessLink_Expiry");
            entity.Property(e => e.TokenHash).HasMaxLength(64).IsUnicode(false);
            entity.Property(e => e.RevocationReason).HasMaxLength(1000);
            entity.Property(e => e.CreatedDate).HasColumnType("datetime2").HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.ActivatedAt).HasColumnType("datetime2");
            entity.Property(e => e.ExpiresAt).HasColumnType("datetime2");
            entity.Property(e => e.RevokedAt).HasColumnType("datetime2");
            entity.Property(e => e.RowVersion).IsRowVersion().IsConcurrencyToken();
        });

        modelBuilder.Entity<TblContractCustomerOtpChallenge>(entity =>
        {
            entity.HasKey(e => e.CustomerOtpChallengeId)
                .HasName("PK_tbl_ContractCustomerOtpChallenge");

            entity.ToTable("tbl_ContractCustomerOtpChallenge", table =>
            {
                table.HasCheckConstraint("CK_tbl_ContractCustomerOtpChallenge_LogicalIds", "[LinkId] > 0 AND [VerificationPhoneId] > 0");
                table.HasCheckConstraint("CK_tbl_ContractCustomerOtpChallenge_PublicId", "LEN(LTRIM(RTRIM([PublicChallengeId]))) BETWEEN 32 AND 64");
                table.HasCheckConstraint("CK_tbl_ContractCustomerOtpChallenge_Purpose", "[Purpose] = 'CustomerAccess'");
                table.HasCheckConstraint("CK_tbl_ContractCustomerOtpChallenge_OtpHash", "LEN(LTRIM(RTRIM([OtpHash]))) = 64");
                table.HasCheckConstraint("CK_tbl_ContractCustomerOtpChallenge_Attempts", "[FailedAttemptCount] BETWEEN 0 AND 5");
                table.HasCheckConstraint("CK_tbl_ContractCustomerOtpChallenge_Expiry", "[ExpiresAt] > [CreatedDate]");
                table.HasCheckConstraint("CK_tbl_ContractCustomerOtpChallenge_Lock", "[LockedAt] IS NULL OR [FailedAttemptCount] = 5");
            });

            entity.HasIndex(e => e.PublicChallengeId).IsUnique().HasDatabaseName("UX_tbl_ContractCustomerOtpChallenge_PublicChallengeId");
            entity.HasIndex(e => new { e.LinkId, e.CreatedDate }).HasDatabaseName("IX_tbl_ContractCustomerOtpChallenge_Link_Created");
            entity.HasIndex(e => new { e.LinkId, e.ExpiresAt }).HasFilter("[UsedAt] IS NULL AND [LockedAt] IS NULL AND [InvalidatedAt] IS NULL").HasDatabaseName("IX_tbl_ContractCustomerOtpChallenge_ActiveLookup");
            entity.Property(e => e.PublicChallengeId).HasMaxLength(64).IsUnicode(false);
            entity.Property(e => e.Purpose).HasMaxLength(32).IsUnicode(false);
            entity.Property(e => e.OtpHash).HasMaxLength(64).IsUnicode(false);
            entity.Property(e => e.ExpiresAt).HasColumnType("datetime2");
            entity.Property(e => e.SentAt).HasColumnType("datetime2");
            entity.Property(e => e.UsedAt).HasColumnType("datetime2");
            entity.Property(e => e.LockedAt).HasColumnType("datetime2");
            entity.Property(e => e.InvalidatedAt).HasColumnType("datetime2");
            entity.Property(e => e.CreatedDate).HasColumnType("datetime2").HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.RowVersion).IsRowVersion().IsConcurrencyToken();
        });

        modelBuilder.Entity<TblContractCustomerAccessSession>(entity =>
        {
            entity.HasKey(e => e.CustomerAccessSessionId)
                .HasName("PK_tbl_ContractCustomerAccessSession");

            entity.ToTable("tbl_ContractCustomerAccessSession", table =>
            {
                table.HasCheckConstraint("CK_tbl_ContractCustomerAccessSession_LogicalIds", "[TenantId] > 0 AND [LinkId] > 0 AND [ContractId] > 0 AND [VersionId] > 0 AND [VerificationPhoneId] > 0");
                table.HasCheckConstraint("CK_tbl_ContractCustomerAccessSession_TokenHash", "LEN(LTRIM(RTRIM([SessionTokenHash]))) = 64");
                table.HasCheckConstraint("CK_tbl_ContractCustomerAccessSession_Expiry", "[IssuedAt] <= [LastActivityAt] AND [IdleExpiresAt] >= [LastActivityAt] AND [HardExpiresAt] >= [IdleExpiresAt]");
                table.HasCheckConstraint("CK_tbl_ContractCustomerAccessSession_Revocation", "[RevokedAt] IS NULL OR LEN(LTRIM(RTRIM([RevocationReason]))) BETWEEN 1 AND 1000");
            });

            entity.HasIndex(e => e.SessionTokenHash).IsUnique().HasDatabaseName("UX_tbl_ContractCustomerAccessSession_TokenHash");
            entity.HasIndex(e => new { e.LinkId, e.RevokedAt, e.IdleExpiresAt }).HasDatabaseName("IX_tbl_ContractCustomerAccessSession_ActiveLink");
            entity.HasIndex(e => new { e.ContractId, e.VersionId, e.RevokedAt }).HasDatabaseName("IX_tbl_ContractCustomerAccessSession_ContractVersion");
            entity.Property(e => e.SessionTokenHash).HasMaxLength(64).IsUnicode(false);
            entity.Property(e => e.RevocationReason).HasMaxLength(1000);
            entity.Property(e => e.IssuedAt).HasColumnType("datetime2");
            entity.Property(e => e.LastActivityAt).HasColumnType("datetime2");
            entity.Property(e => e.IdleExpiresAt).HasColumnType("datetime2");
            entity.Property(e => e.HardExpiresAt).HasColumnType("datetime2");
            entity.Property(e => e.RevokedAt).HasColumnType("datetime2");
            entity.Property(e => e.RowVersion).IsRowVersion().IsConcurrencyToken();
        });

        modelBuilder.Entity<TblContractCustomerOtpDeliveryOutbox>(entity =>
        {
            entity.HasKey(e => e.CustomerOtpDeliveryOutboxId)
                .HasName("PK_tbl_ContractCustomerOtpDeliveryOutbox");

            entity.ToTable("tbl_ContractCustomerOtpDeliveryOutbox", table =>
            {
                table.HasCheckConstraint("CK_tbl_ContractCustomerOtpDeliveryOutbox_Challenge", "[ChallengeId] > 0");
                table.HasCheckConstraint("CK_tbl_ContractCustomerOtpDeliveryOutbox_Status", "[Status] IN ('Pending', 'Leased', 'Sent', 'Failed')");
                table.HasCheckConstraint("CK_tbl_ContractCustomerOtpDeliveryOutbox_Attempts", "[AttemptCount] >= 0");
                table.HasCheckConstraint("CK_tbl_ContractCustomerOtpDeliveryOutbox_Payload", "LEN(LTRIM(RTRIM([EncryptedPayload]))) > 0");
            });

            entity.HasIndex(e => e.ChallengeId).IsUnique().HasDatabaseName("UX_tbl_ContractCustomerOtpDeliveryOutbox_ChallengeId");
            entity.HasIndex(e => new { e.Status, e.NextAttemptAt, e.LeaseUntil }).HasDatabaseName("IX_tbl_ContractCustomerOtpDeliveryOutbox_Lease");
            entity.Property(e => e.EncryptedPayload).HasColumnType("nvarchar(max)");
            entity.Property(e => e.Status).HasMaxLength(16).IsUnicode(false);
            entity.Property(e => e.LeaseId).HasMaxLength(64).IsUnicode(false);
            entity.Property(e => e.LastFailure).HasMaxLength(1000);
            entity.Property(e => e.NextAttemptAt).HasColumnType("datetime2");
            entity.Property(e => e.LeaseUntil).HasColumnType("datetime2");
            entity.Property(e => e.SentAt).HasColumnType("datetime2");
            entity.Property(e => e.FailedAt).HasColumnType("datetime2");
            entity.Property(e => e.CreatedDate).HasColumnType("datetime2").HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.RowVersion).IsRowVersion().IsConcurrencyToken();
        });

        modelBuilder.Entity<TblContractAppendix>(entity =>
        {
            entity.HasKey(e => e.AppendixId).HasName("PK__tbl_Cont__44B149C44BC57074");

            entity.ToTable("tbl_ContractAppendix");

            entity.Property(e => e.AppendixCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.AppendixDate).HasColumnType("datetime");
            entity.Property(e => e.AppendixDescription).HasMaxLength(2000);
            entity.Property(e => e.AppendixName).HasMaxLength(1000);
            entity.Property(e => e.AppendixNameEn)
                .HasMaxLength(500)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TblContractAttachment>(entity =>
        {
            entity.HasKey(e => e.AttachmentId).HasName("PK__tbl_Cont__442C64BE6AC2F4CE");

            entity.ToTable("tbl_ContractAttachment");

            entity.Property(e => e.ContractFileName)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.ContractFilePath)
                .HasMaxLength(1000)
                .IsUnicode(false);
            entity.Property(e => e.UploadDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.DocumentType)
                .HasDefaultValue((byte)99);
        });

        modelBuilder.Entity<TblContractTemplate>(entity =>
        {
            entity.HasKey(e => e.TemplateId)
                .HasName("PK_tbl_ContractTemplate");

            entity.ToTable("tbl_ContractTemplate", table =>
            {
                table.HasCheckConstraint(
                    "CK_tbl_ContractTemplate_TemplateCode",
                    "LEN(LTRIM(RTRIM([TemplateCode]))) > 0");

                // Các giá trị thuộc TemplateDocumentType.
                table.HasCheckConstraint(
                    "CK_tbl_ContractTemplate_DocumentType",
                    "[DocumentType] IN (1, 2, 3, 4, 5, 6, 7, 8, 99)");

                // ContractLanguageMode: Vietnamese hoặc Bilingual.
                table.HasCheckConstraint(
                    "CK_tbl_ContractTemplate_LanguageMode",
                    "[LanguageMode] IN (1, 2)");

                table.HasCheckConstraint(
                    "CK_tbl_ContractTemplate_CurrentPublishedVersionId",
                    "[CurrentPublishedVersionId] IS NULL " +
                    "OR [CurrentPublishedVersionId] > 0");
            });

            entity.HasIndex(e => e.TemplateCode)
                .IsUnique()
                .HasDatabaseName("UX_tbl_ContractTemplate_TemplateCode");

            entity.HasIndex(e => new { e.DocumentType, e.IsActive })
                .HasDatabaseName(
                    "IX_tbl_ContractTemplate_DocumentType_IsActive");

            entity.HasIndex(e => e.CurrentPublishedVersionId)
                .HasDatabaseName(
                    "IX_tbl_ContractTemplate_CurrentPublishedVersionId");

            entity.Property(e => e.TemplateCode)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.Property(e => e.TemplateName)
                .HasMaxLength(500);

            entity.Property(e => e.TemplateNameEn)
                .HasMaxLength(500);

            entity.Property(e => e.Description)
                .HasMaxLength(2000);

            entity.Property(e => e.IsActive)
                .HasDefaultValue(
                    true,
                    "DF_tbl_ContractTemplate_IsActive");

            entity.Property(e => e.CreatedDate)
                .HasColumnType("datetime2")
                .HasDefaultValueSql(
                    "(sysutcdatetime())",
                    "DF_tbl_ContractTemplate_CreatedDate");

            entity.Property(e => e.UpdatedDate)
                .HasColumnType("datetime2");

            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
        });

        modelBuilder.Entity<TblContractTemplateVersion>(entity =>
        {
            entity.HasKey(e => e.TemplateVersionId)
                .HasName("PK_tbl_ContractTemplateVersion");

            entity.ToTable("tbl_ContractTemplateVersion", table =>
            {
                table.HasCheckConstraint(
                    "CK_tbl_ContractTemplateVersion_VersionNo",
                    "[VersionNo] > 0");

                table.HasCheckConstraint(
                    "CK_tbl_ContractTemplateVersion_Status",
                    "[Status] IN (0, 1, 2)");

                table.HasCheckConstraint(
                    "CK_tbl_ContractTemplateVersion_ValidationStatus",
                    "[ValidationStatus] IN (0, 1, 2)");

                /*
                 * Nếu chưa upload DOCX thì cả FileId và Hash đều null.
                 * Nếu đã upload thì FileId phải dương và hash dài 64 ký tự.
                 */
                table.HasCheckConstraint(
                    "CK_tbl_ContractTemplateVersion_Document",
                    "([DocumentFileId] IS NULL AND [DocumentHash] IS NULL) " +
                    "OR " +
                    "([DocumentFileId] > 0 " +
                    "AND [DocumentHash] IS NOT NULL " +
                    "AND LEN([DocumentHash]) = 64)");

                /*
                 * NotValidated: chưa có thông tin người/thời điểm validate.
                 * Valid: đã lưu người và thời điểm validate.
                 * Invalid: ngoài audit còn phải có thông báo lỗi.
                 */
                table.HasCheckConstraint(
                    "CK_tbl_ContractTemplateVersion_Validation",
                    "([ValidationStatus] = 0 " +
                    "AND [ValidatedByEmployeeId] IS NULL " +
                    "AND [ValidatedDate] IS NULL " +
                    "AND [ValidationMessage] IS NULL) " +
                    "OR " +
                    "([ValidationStatus] = 1 " +
                    "AND [ValidatedByEmployeeId] IS NOT NULL " +
                    "AND [ValidatedDate] IS NOT NULL) " +
                    "OR " +
                    "([ValidationStatus] = 2 " +
                    "AND [ValidatedByEmployeeId] IS NOT NULL " +
                    "AND [ValidatedDate] IS NOT NULL " +
                    "AND LEN(LTRIM(RTRIM(" +
                    "COALESCE([ValidationMessage], N'')))) > 0)");

                /*
                 * Draft chưa có publish audit.
                 * Published/Retired phải từng được publish hợp lệ.
                 */
                table.HasCheckConstraint(
                    "CK_tbl_ContractTemplateVersion_PublishState",
                    "([Status] = 0 " +
                    "AND [PublishedByEmployeeId] IS NULL " +
                    "AND [PublishedDate] IS NULL) " +
                    "OR " +
                    "([Status] IN (1, 2) " +
                    "AND [ValidationStatus] = 1 " +
                    "AND [DocumentFileId] IS NOT NULL " +
                    "AND [DocumentHash] IS NOT NULL " +
                    "AND [PublishedByEmployeeId] IS NOT NULL " +
                    "AND [PublishedDate] IS NOT NULL)");
            });

            entity.HasIndex(e => new { e.TemplateId, e.VersionNo })
                .IsUnique()
                .HasDatabaseName(
                    "UX_tbl_ContractTemplateVersion_TemplateId_VersionNo");

            entity.HasIndex(e => new { e.TemplateId, e.Status })
                .HasDatabaseName(
                    "IX_tbl_ContractTemplateVersion_TemplateId_Status");

            /*
             * Một FileStorage record chỉ đại diện cho DOCX
             * của một template version.
             */
            entity.HasIndex(e => e.DocumentFileId)
                .IsUnique()
                .HasFilter("[DocumentFileId] IS NOT NULL")
                .HasDatabaseName(
                    "UX_tbl_ContractTemplateVersion_DocumentFileId");

            entity.Property(e => e.ChangeNote)
                .HasMaxLength(2000);

            entity.Property(e => e.Status)
                .HasDefaultValue(
                    (byte)0,
                    "DF_tbl_ContractTemplateVersion_Status");

            entity.Property(e => e.ValidationStatus)
                .HasDefaultValue(
                    (byte)0,
                    "DF_tbl_ContractTemplateVersion_ValidationStatus");

            entity.Property(e => e.ValidationMessage)
                .HasMaxLength(4000);

            entity.Property(e => e.DocumentHash)
                .HasMaxLength(64)
                .IsUnicode(false)
                .IsFixedLength();

            entity.Property(e => e.ValidatedDate)
                .HasColumnType("datetime2");

            entity.Property(e => e.PublishedDate)
                .HasColumnType("datetime2");

            entity.Property(e => e.CreatedDate)
                .HasColumnType("datetime2")
                .HasDefaultValueSql(
                    "(sysutcdatetime())",
                    "DF_tbl_ContractTemplateVersion_CreatedDate");

            entity.Property(e => e.UpdatedDate)
                .HasColumnType("datetime2");

            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
        });

        modelBuilder.Entity<TblContractTemplateField>(entity =>
        {
            entity.HasKey(e => e.TemplateFieldId)
                .HasName("PK_tbl_ContractTemplateField");

            entity.ToTable("tbl_ContractTemplateField", table =>
            {
                // Logical reference phải là ID hợp lệ.
                table.HasCheckConstraint(
                    "CK_tbl_ContractTemplateField_TemplateVersionId",
                    "[TemplateVersionId] > 0");

                // Placeholder không được rỗng hoặc chỉ chứa khoảng trắng.
                table.HasCheckConstraint(
                    "CK_tbl_ContractTemplateField_PlaceholderKey",
                    "LEN(LTRIM(RTRIM([PlaceholderKey]))) > 0");

                // Nguồn dữ liệu không được rỗng.
                table.HasCheckConstraint(
                    "CK_tbl_ContractTemplateField_DataSource",
                    "LEN(LTRIM(RTRIM([DataSource]))) > 0");

                // Thứ tự hiển thị không được âm.
                table.HasCheckConstraint(
                    "CK_tbl_ContractTemplateField_DisplayOrder",
                    "[DisplayOrder] >= 0");
            });

            /*
             * Trong cùng một template version,
             * mỗi placeholder chỉ được khai báo một lần.
             */
            entity.HasIndex(e => new
            {
                e.TemplateVersionId,
                e.PlaceholderKey
            })
                .IsUnique()
                .HasDatabaseName(
                    "UX_tbl_ContractTemplateField_Version_Placeholder");

            // Phục vụ lấy danh sách field theo đúng thứ tự.
            entity.HasIndex(e => new
            {
                e.TemplateVersionId,
                e.DisplayOrder
            })
                .HasDatabaseName(
                    "IX_tbl_ContractTemplateField_Version_DisplayOrder");

            entity.Property(e => e.PlaceholderKey)
                .HasMaxLength(100)
                .IsUnicode(false)
                .IsRequired();

            entity.Property(e => e.FieldLabel)
                .HasMaxLength(300)
                .IsRequired();

            entity.Property(e => e.DataSource)
                .HasMaxLength(500)
                .IsUnicode(false)
                .IsRequired();

            entity.Property(e => e.DefaultValue)
                .HasMaxLength(2000);

            entity.Property(e => e.FormatString)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.Property(e => e.IsRequired)
                .HasDefaultValue(
                    false,
                    "DF_tbl_ContractTemplateField_IsRequired");

            entity.Property(e => e.DisplayOrder)
                .HasDefaultValue(
                    0,
                    "DF_tbl_ContractTemplateField_DisplayOrder");

            entity.Property(e => e.CreatedDate)
                .HasColumnType("datetime2")
                .HasDefaultValueSql(
                    "(sysutcdatetime())",
                    "DF_tbl_ContractTemplateField_CreatedDate");

            entity.Property(e => e.UpdatedDate)
                .HasColumnType("datetime2");

            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
        });

        modelBuilder.Entity<TblContractTemplateTerm>(entity =>
        {
            entity.HasKey(e => e.TemplateTermId)
                .HasName("PK_tbl_ContractTemplateTerm");

            entity.ToTable("tbl_ContractTemplateTerm", table =>
            {
                // Logical reference phải là ID hợp lệ.
                table.HasCheckConstraint(
                    "CK_tbl_ContractTemplateTerm_TemplateVersionId",
                    "[TemplateVersionId] > 0");

                // Mã điều khoản không được rỗng.
                table.HasCheckConstraint(
                    "CK_tbl_ContractTemplateTerm_TermCode",
                    "LEN(LTRIM(RTRIM([TermCode]))) > 0");

                // Thứ tự hiển thị không được âm.
                table.HasCheckConstraint(
                    "CK_tbl_ContractTemplateTerm_DisplayOrder",
                    "[DisplayOrder] >= 0");
            });

            /*
             * Một mã điều khoản chỉ xuất hiện một lần
             * trong cùng một template version.
             */
            entity.HasIndex(e => new
            {
                e.TemplateVersionId,
                e.TermCode
            })
                .IsUnique()
                .HasDatabaseName(
                    "UX_tbl_ContractTemplateTerm_Version_TermCode");

            // Phục vụ tải điều khoản theo thứ tự hiển thị.
            entity.HasIndex(e => new
            {
                e.TemplateVersionId,
                e.DisplayOrder
            })
                .HasDatabaseName(
                    "IX_tbl_ContractTemplateTerm_Version_DisplayOrder");

            entity.Property(e => e.TermCode)
                .HasMaxLength(100)
                .IsUnicode(false)
                .IsRequired();

            entity.Property(e => e.TermTitle)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(e => e.TermTitleEn)
                .HasMaxLength(500);

            /*
             * Không giới hạn MaxLength cho nội dung điều khoản.
             * SQL Server sẽ sử dụng nvarchar(max).
             */
            entity.Property(e => e.TermContent);

            entity.Property(e => e.TermContentEn);

            // Mặc định khóa đàm phán để an toàn.
            entity.Property(e => e.IsNegotiable)
                .HasDefaultValue(
                    false,
                    "DF_tbl_ContractTemplateTerm_IsNegotiable");

            entity.Property(e => e.DisplayOrder)
                .HasDefaultValue(
                    0,
                    "DF_tbl_ContractTemplateTerm_DisplayOrder");

            entity.Property(e => e.CreatedDate)
                .HasColumnType("datetime2")
                .HasDefaultValueSql(
                    "(sysutcdatetime())",
                    "DF_tbl_ContractTemplateTerm_CreatedDate");

            entity.Property(e => e.UpdatedDate)
                .HasColumnType("datetime2");

            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
        });

        modelBuilder.Entity<TblContractTerm>(entity =>
        {
            entity.HasKey(e => e.TermId)
                .HasName("PK_tbl_ContractTerm");

            entity.ToTable("tbl_ContractTerm", table =>
            {
                // Contract sở hữu term phải là logical ID hợp lệ.
                table.HasCheckConstraint(
                    "CK_tbl_ContractTerm_ContractId",
                    "[ContractId] > 0");

                // Mỗi term bắt buộc phải thuộc một contract version.
                table.HasCheckConstraint(
                    "CK_tbl_ContractTerm_VersionId",
                    "[VersionId] > 0");

                // Term nhập thủ công có thể không có nguồn template.
                // Nếu có nguồn thì ID phải hợp lệ.
                table.HasCheckConstraint(
                    "CK_tbl_ContractTerm_SourceTemplateTermId",
                    "[SourceTemplateTermId] IS NULL OR [SourceTemplateTermId] > 0");

                // Không chấp nhận mã term rỗng hoặc chỉ chứa khoảng trắng.
                table.HasCheckConstraint(
                    "CK_tbl_ContractTerm_TermCode",
                    "LEN(LTRIM(RTRIM([TermCode]))) > 0");

                // Thứ tự hiển thị không được âm.
                table.HasCheckConstraint(
                    "CK_tbl_ContractTerm_DisplayOrder",
                    "[DisplayOrder] >= 0");
            });

            /*
             * Trong một version, mỗi TermCode chỉ được xuất hiện một lần.
             * Version khác vẫn được phép có cùng TermCode vì đó là snapshot mới.
             */
            entity.HasIndex(e => new { e.VersionId, e.TermCode })
                .IsUnique()
                .HasDatabaseName("UX_tbl_ContractTerm_VersionId_TermCode");

            // Phục vụ lấy danh sách term đúng thứ tự của một contract/version.
            entity.HasIndex(e => new
            {
                e.ContractId,
                e.VersionId,
                e.DisplayOrder
            })
                .HasDatabaseName(
                    "IX_tbl_ContractTerm_ContractId_VersionId_DisplayOrder");

            entity.HasIndex(e => e.SourceTemplateTermId)
                .HasDatabaseName("IX_tbl_ContractTerm_SourceTemplateTermId");

            entity.Property(e => e.TermCode)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.Property(e => e.TermTitle)
                .HasMaxLength(500);

            entity.Property(e => e.TermTitleEn)
                .HasMaxLength(500);

            entity.Property(e => e.TermContent)
                .HasColumnType("nvarchar(max)");

            entity.Property(e => e.TermContentEn)
                .HasColumnType("nvarchar(max)");

            /*
             * Mặc định đóng:
             * nếu người tạo template quên cấu hình, khách hàng không được
             * comment nhầm vào điều khoản cứng.
             */
            entity.Property(e => e.IsNegotiable)
                .HasDefaultValue(
                    false,
                    "DF_tbl_ContractTerm_IsNegotiable");

            entity.Property(e => e.DisplayOrder)
                .HasDefaultValue(
                    0,
                    "DF_tbl_ContractTerm_DisplayOrder");

            entity.Property(e => e.CreatedDate)
                .HasColumnType("datetime2")
                .HasDefaultValueSql(
                    "(sysutcdatetime())",
                    "DF_tbl_ContractTerm_CreatedDate");

            entity.Property(e => e.UpdatedDate)
                .HasColumnType("datetime2");

            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
        });

        modelBuilder.Entity<TblContractItem>(entity =>
        {
            entity.HasKey(e => e.ContractItemId)
                .HasName("PK_tbl_ContractItem");

            entity.ToTable("tbl_ContractItem", table =>
            {
                // Contract và Version phải là logical ID hợp lệ.
                table.HasCheckConstraint(
                    "CK_tbl_ContractItem_ContractId",
                    "[ContractId] > 0");

                table.HasCheckConstraint(
                    "CK_tbl_ContractItem_VersionId",
                    "[VersionId] > 0");

                // 1 = Product, 2 = Service.
                table.HasCheckConstraint(
                    "CK_tbl_ContractItem_ItemType",
                    "[ItemType] IN (1, 2)");

                /*
                 * Product không được tham chiếu Service.
                 * Service không được tham chiếu Product.
                 * Cả hai Source ID đều null thì là item nhập ngoài catalog.
                 */
                table.HasCheckConstraint(
                    "CK_tbl_ContractItem_SourceByType",
                    "([ItemType] = 1 AND [SourceServiceId] IS NULL) " +
                    "OR ([ItemType] = 2 AND [SourceProductId] IS NULL)");

                table.HasCheckConstraint(
                    "CK_tbl_ContractItem_SourceProductId",
                    "[SourceProductId] IS NULL OR [SourceProductId] > 0");

                table.HasCheckConstraint(
                    "CK_tbl_ContractItem_SourceServiceId",
                    "[SourceServiceId] IS NULL OR [SourceServiceId] > 0");

                // Tên snapshot bắt buộc phải có nội dung.
                table.HasCheckConstraint(
                    "CK_tbl_ContractItem_ItemName",
                    "LEN(LTRIM(RTRIM([ItemName]))) > 0");

                table.HasCheckConstraint(
                    "CK_tbl_ContractItem_Quantity",
                    "[Quantity] > 0");

                table.HasCheckConstraint(
                    "CK_tbl_ContractItem_UnitPrice",
                    "[UnitPrice] >= 0");

                table.HasCheckConstraint(
                    "CK_tbl_ContractItem_LineSubtotal",
                    "[LineSubtotal] >= 0");

                table.HasCheckConstraint(
                    "CK_tbl_ContractItem_DiscountPercent",
                    "[DiscountPercent] >= 0 AND [DiscountPercent] <= 100");

                table.HasCheckConstraint(
                    "CK_tbl_ContractItem_DiscountMode",
                    "[DiscountMode] IN (0, 1, 2)");

                table.HasCheckConstraint(
                    "CK_tbl_ContractItem_DiscountInputs",
                    "([DiscountMode] = 0 AND [DiscountPercent] = 0 " +
                    "AND [FixedDiscountAmount] = 0) OR " +
                    "([DiscountMode] = 1 AND [FixedDiscountAmount] = 0) OR " +
                    "([DiscountMode] = 2 AND [DiscountPercent] = 0 " +
                    "AND [FixedDiscountAmount] <= [LineSubtotal])");

                table.HasCheckConstraint(
                    "CK_tbl_ContractItem_DiscountAmount",
                    "[DiscountAmount] >= 0 AND [DiscountAmount] <= [LineSubtotal]");

                table.HasCheckConstraint(
                    "CK_tbl_ContractItem_VatPercent",
                    "[VatPercent] >= 0 AND [VatPercent] <= 100");

                table.HasCheckConstraint(
                    "CK_tbl_ContractItem_VatAmount",
                    "[VatAmount] >= 0");

                table.HasCheckConstraint(
                    "CK_tbl_ContractItem_TaxableVat",
                    "[IsTaxable] = 1 OR ([VatPercent] = 0 AND [VatAmount] = 0)");

                table.HasCheckConstraint(
                    "CK_tbl_ContractItem_LineTotal",
                    "[LineTotal] >= 0");

                table.HasCheckConstraint(
                    "CK_tbl_ContractItem_DisplayOrder",
                    "[DisplayOrder] >= 0");

                table.HasCheckConstraint(
                    "CK_tbl_ContractItem_CreatedEmployeeId",
                    "[CreatedEmployeeId] > 0");

                table.HasCheckConstraint(
                    "CK_tbl_ContractItem_UpdatedEmployeeId",
                    "[UpdatedEmployeeId] IS NULL OR [UpdatedEmployeeId] > 0");
            });

            // Lấy item của một version theo đúng thứ tự hiển thị.
            entity.HasIndex(e => new
            {
                e.VersionId,
                e.DisplayOrder
            })
                .HasDatabaseName(
                    "IX_tbl_ContractItem_Version_DisplayOrder");

            // Hỗ trợ truy vấn các item theo hợp đồng và version.
            entity.HasIndex(e => new
            {
                e.ContractId,
                e.VersionId
            })
                .HasDatabaseName(
                    "IX_tbl_ContractItem_Contract_Version");

            entity.Property(e => e.ItemCode)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.Property(e => e.ItemName)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(e => e.ItemNameEn)
                .HasMaxLength(500);

            // Không giới hạn độ dài mô tả:
            // SQL Server sẽ sử dụng nvarchar(max).
            entity.Property(e => e.ItemDescription);

            entity.Property(e => e.ItemDescriptionEn);

            entity.Property(e => e.UnitName)
                .HasMaxLength(100);

            entity.Property(e => e.UnitNameEn)
                .HasMaxLength(100);

            // Hỗ trợ số lượng lẻ, ví dụ 1.5 tháng hoặc 2.25 đơn vị.
            entity.Property(e => e.Quantity)
                .HasPrecision(18, 4)
                .HasDefaultValue(
                    1m,
                    "DF_tbl_ContractItem_Quantity");

            // Các giá trị tiền dùng chính xác 2 chữ số thập phân.
            entity.Property(e => e.UnitPrice)
                .HasPrecision(18, 2);

            entity.Property(e => e.LineSubtotal)
                .HasPrecision(18, 2);

            entity.Property(e => e.DiscountPercent)
                .HasPrecision(5, 2)
                .HasDefaultValue(
                    0m,
                    "DF_tbl_ContractItem_DiscountPercent");

            entity.Property(e => e.DiscountMode)
                .HasDefaultValue(
                    (byte)0,
                    "DF_tbl_ContractItem_DiscountMode");

            entity.Property(e => e.FixedDiscountAmount)
                .HasPrecision(18, 2)
                .HasDefaultValue(
                    0m,
                    "DF_tbl_ContractItem_FixedDiscountAmount");

            entity.Property(e => e.DiscountAmount)
                .HasPrecision(18, 2)
                .HasDefaultValue(
                    0m,
                    "DF_tbl_ContractItem_DiscountAmount");

            entity.Property(e => e.VatPercent)
                .HasPrecision(5, 2)
                .HasDefaultValue(
                    0m,
                    "DF_tbl_ContractItem_VatPercent");

            entity.Property(e => e.IsTaxable)
                .HasDefaultValue(
                    true,
                    "DF_tbl_ContractItem_IsTaxable");

            entity.Property(e => e.VatAmount)
                .HasPrecision(18, 2)
                .HasDefaultValue(
                    0m,
                    "DF_tbl_ContractItem_VatAmount");

            entity.Property(e => e.LineTotal)
                .HasPrecision(18, 2);

            entity.Property(e => e.DisplayOrder)
                .HasDefaultValue(
                    0,
                    "DF_tbl_ContractItem_DisplayOrder");

            entity.Property(e => e.CreatedDate)
                .HasColumnType("datetime2")
                .HasDefaultValueSql(
                    "(sysutcdatetime())",
                    "DF_tbl_ContractItem_CreatedDate");

            entity.Property(e => e.UpdatedDate)
                .HasColumnType("datetime2");

            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
        });

        modelBuilder.Entity<TblContractVersion>(entity =>
        {
            entity.HasKey(e => e.VersionId)
                .HasName("PK_tbl_ContractVersion");

            entity.ToTable("tbl_ContractVersion", table =>
            {
                // Contract sở hữu version phải là logical ID hợp lệ.
                table.HasCheckConstraint(
                    "CK_tbl_ContractVersion_ContractId",
                    "[ContractId] > 0");

                // Version nguồn có thể null đối với version đầu tiên.
                // Nếu có thì phải là logical ID hợp lệ.
                table.HasCheckConstraint(
                    "CK_tbl_ContractVersion_SourceVersionId",
                    "[SourceVersionId] IS NULL OR [SourceVersionId] > 0");

                // TemplateVersionId được phép null với hợp đồng legacy.
                // Nếu có thì phải là logical ID hợp lệ.
                table.HasCheckConstraint(
                    "CK_tbl_ContractVersion_TemplateVersionId",
                    "[TemplateVersionId] IS NULL OR [TemplateVersionId] > 0");

                // Version bắt đầu từ 1, không chấp nhận 0 hoặc số âm.
                table.HasCheckConstraint(
                    "CK_tbl_ContractVersion_VersionNo",
                    "[VersionNo] > 0");

                table.HasCheckConstraint(
                    "CK_tbl_ContractVersion_CurrencyCode",
                    "[CurrencyCode] IN ('VND', 'USD')");

                table.HasCheckConstraint(
                    "CK_tbl_ContractVersion_FinancialTotals",
                    "[Subtotal] >= 0 AND [TotalDiscount] >= 0 " +
                    "AND [TotalVat] >= 0 AND [TotalAmount] >= 0");

                /*
                 * Version chưa khóa:
                 * - không có LockedDate;
                 * - không có LockedByEmployeeId.
                 *
                 * Version đã khóa:
                 * - phải biết thời điểm/người khóa;
                 * - phải có snapshot và hash để xác định nội dung bất biến.
                 */
                table.HasCheckConstraint(
                    "CK_tbl_ContractVersion_LockState",
                    "([IsLocked] = 0 " +
                    "AND [LockedDate] IS NULL " +
                    "AND [LockedByEmployeeId] IS NULL) " +
                    "OR " +
                    "([IsLocked] = 1 " +
                    "AND [LockedDate] IS NOT NULL " +
                    "AND [LockedByEmployeeId] IS NOT NULL " +
                    "AND [SnapshotJson] IS NOT NULL " +
                    "AND [SnapshotHash] IS NOT NULL)");
            });

            // Một hợp đồng không thể có hai version cùng VersionNo.
            entity.HasIndex(e => new { e.ContractId, e.VersionNo })
                .IsUnique()
                .HasDatabaseName("UX_tbl_ContractVersion_ContractId_VersionNo");

            entity.HasIndex(e => e.SourceVersionId)
                .HasDatabaseName("IX_tbl_ContractVersion_SourceVersionId");

            entity.HasIndex(e => e.TemplateVersionId)
                .HasDatabaseName("IX_tbl_ContractVersion_TemplateVersionId");

            entity.Property(e => e.ChangeNote)
                .HasMaxLength(2000);

            entity.Property(e => e.CurrencyCode)
                .HasMaxLength(3)
                .IsUnicode(false)
                .IsFixedLength()
                .HasDefaultValue("VND");

            entity.Property(e => e.Subtotal)
                .HasPrecision(18, 2)
                .HasDefaultValue(0m);

            entity.Property(e => e.TotalDiscount)
                .HasPrecision(18, 2)
                .HasDefaultValue(0m);

            entity.Property(e => e.TotalVat)
                .HasPrecision(18, 2)
                .HasDefaultValue(0m);

            entity.Property(e => e.TotalAmount)
                .HasPrecision(18, 2)
                .HasDefaultValue(0m);

            entity.Property(e => e.SnapshotJson)
                .HasColumnType("nvarchar(max)");

            entity.Property(e => e.SnapshotHash)
                .HasMaxLength(64)
                .IsUnicode(false)
                .IsFixedLength();

            entity.Property(e => e.IsLocked)
                .HasDefaultValue(false, "DF_tbl_ContractVersion_IsLocked");

            entity.Property(e => e.LockedDate)
                .HasColumnType("datetime2");

            entity.Property(e => e.CreatedDate)
                .HasColumnType("datetime2")
                .HasDefaultValueSql(
                    "(sysutcdatetime())",
                    "DF_tbl_ContractVersion_CreatedDate");

            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
        });

        modelBuilder.Entity<TblCustomer>(entity =>
        {
            entity.HasKey(e => e.CustomerId).HasName("PK__tbl_Cust__A4AE64D84BC5AE01");

            entity.ToTable("tbl_Customers");

            entity.Property(e => e.CustomerAccount)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CustomerAddress).HasMaxLength(2000);
            entity.Property(e => e.CustomerBirthday).HasColumnType("datetime");
            entity.Property(e => e.CustomerCity).HasMaxLength(1000);
            entity.Property(e => e.CustomerCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.CustomerComments).HasMaxLength(2000);
            entity.Property(e => e.CustomerCompany).HasMaxLength(1000);
            entity.Property(e => e.CustomerCountry).HasMaxLength(200);
            entity.Property(e => e.CustomerEmail)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CustomerFaxNumber)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.CustomerFullName).HasMaxLength(100);
            entity.Property(e => e.CustomerImageIcon)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CustomerMobile)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.CustomerNotes).HasMaxLength(2000);
            entity.Property(e => e.CustomerPassword)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CustomerPhone)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.CustomerTaxCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.CustomerWebsite).HasMaxLength(500);
            entity.Property(e => e.CustomerZipCode)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.DateCreated)
                .HasDefaultValueSql("(getdate())", "DF_tbl_Customers_DateCreated")
                .HasColumnType("datetime");
            entity.Property(e => e.DateModified).HasColumnType("datetime");
            entity.Property(e => e.Status).HasDefaultValue((byte)0, "DF_tbl_Customers_Status");
        });

        modelBuilder.Entity<TblCustomerInteraction>(entity =>
        {
            entity.HasKey(e => e.InteractionId).HasName("PK__tbl_Cust__922C0496FCECB578");

            entity.ToTable("tbl_CustomerInteraction");

            entity.Property(e => e.InteractionDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.InteractionSubject).HasMaxLength(1000);
            entity.Property(e => e.InteractionType)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.NextFollowUpDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<TblDeliveryDetail>(entity =>
        {
            entity.HasKey(e => e.DeliveryDetailId).HasName("PK__tbl_Deli__EFD2C2E7914C480F");

            entity.ToTable("tbl_DeliveryDetail");

            entity.Property(e => e.Note).HasMaxLength(1000);
        });

        modelBuilder.Entity<TblDeliveryOrder>(entity =>
        {
            entity.HasKey(e => e.DeliveryId).HasName("PK__tbl_Deli__626D8FCEF0133037");

            entity.ToTable("tbl_DeliveryOrder");

            entity.Property(e => e.DeliveryAddress).HasMaxLength(1000);
            entity.Property(e => e.DeliveryDate).HasColumnType("datetime");
            entity.Property(e => e.DeliveryNo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.DeliveryStatus)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValue("Pending");
            entity.Property(e => e.ReceiverName).HasMaxLength(200);
            entity.Property(e => e.ReceiverPhone)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TblDepartment>(entity =>
        {
            entity.HasKey(e => e.DepartmentId).HasName("PK_Department_DepartmentID");

            entity.ToTable("tbl_Department");

            entity.Property(e => e.DepartmentId).HasColumnName("DepartmentID");
            entity.Property(e => e.DepartmentCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.DepartmentName).HasMaxLength(200);
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<TblEmployee>(entity =>
        {
            entity.HasKey(e => e.EmployeeId).HasName("PK__tbl_Empl__7AD04F11C415E4E3");

            entity.ToTable("tbl_Employee");

            entity.Property(e => e.DateCreated).HasColumnType("datetime");
            entity.Property(e => e.DateModified).HasColumnType("datetime");
            entity.Property(e => e.DefaultPage)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.EmployeeAccount)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.EmployeeAddress).HasMaxLength(500);
            entity.Property(e => e.EmployeeBirthDate).HasColumnType("datetime");
            entity.Property(e => e.EmployeeCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.EmployeeEmail)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.EmployeeFullName).HasMaxLength(100);
            entity.Property(e => e.EmployeeImageIcon)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.EmployeeMobile)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.EmployeePassword)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.EmployeePhone)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.Gender)
                .HasMaxLength(1)
                .IsFixedLength();
            entity.Property(e => e.HireDate).HasColumnType("datetime");
            entity.Property(e => e.MaritalStatus)
                .HasMaxLength(1)
                .IsFixedLength();
            entity.Property(e => e.Others).HasMaxLength(2000);
            entity.Property(e => e.UserRoles)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TblFileStorage>(entity =>
        {
            entity.HasKey(e => e.FileId).HasName("PK__tbl_File__6F0F98BF777CA8CB");

            entity.ToTable("tbl_FileStorage");

            entity.Property(e => e.FileName).HasMaxLength(300);
            entity.Property(e => e.FilePath).HasMaxLength(1000);
            entity.Property(e => e.FileType)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.ObjectType)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UploadedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
        });

        modelBuilder.Entity<TblInvoice>(entity =>
        {
            entity.HasKey(e => e.InvoiceId).HasName("PK__tbl_Invo__D796AAB5EDC7461D");

            entity.ToTable("tbl_Invoice");

            entity.Property(e => e.InvoiceDate).HasColumnType("datetime");
            entity.Property(e => e.InvoiceNo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InvoiceStatus)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValue("Unpaid");
        });

        modelBuilder.Entity<TblNotification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__tbl_Noti__20CF2E127E71675E");

            entity.ToTable("tbl_Notification");

            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.NotificationTitle).HasMaxLength(300);
            entity.Property(e => e.ObjectType)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TblOrder>(entity =>
        {
            entity.HasKey(e => e.OrderId);

            entity.ToTable("tbl_Orders");

            entity.Property(e => e.DateCreated)
                .HasDefaultValueSql("(getdate())", "DF_tbl_Orders_DateCreated")
                .HasColumnType("datetime");
            entity.Property(e => e.DateExpired).HasColumnType("datetime");
            entity.Property(e => e.NoteFromAdmin).HasMaxLength(1000);
            entity.Property(e => e.OrderComment).HasMaxLength(4000);
            entity.Property(e => e.OrderStatus)
                .HasComment("0: Pending, 1: Approved")
                .HasDefaultValue((byte)0, "DF_tbl_Orders_OrderStatus");
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<TblOrderDetail>(entity =>
        {
            entity.HasKey(e => e.OrderDetailsId);

            entity.ToTable("tbl_OrderDetails");

            entity.Property(e => e.DateCreated)
                .HasDefaultValueSql("(getdate())", "DF_tbl_OrderDetails_DateCreated")
                .HasColumnType("datetime");
            entity.Property(e => e.DateExpired).HasColumnType("datetime");
            entity.Property(e => e.NameDetails).HasMaxLength(2000);
            entity.Property(e => e.StartDate).HasColumnType("datetime");
            entity.Property(e => e.Status).HasDefaultValue((byte)0, "DF_tbl_OrderDetails_Status");
        });

        modelBuilder.Entity<TblPayment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__tbl_Paym__9B556A380D56A7E2");

            entity.ToTable("tbl_Payment");

            entity.Property(e => e.PaymentDate).HasColumnType("datetime");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ReferenceNo)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TblPaymentSchedule>(entity =>
        {
            entity.HasKey(e => e.ScheduleId).HasName("PK__tbl_Paym__9C8A5B49EE0F0668");

            entity.ToTable("tbl_PaymentSchedule");

            entity.Property(e => e.DueDate).HasColumnType("datetime");
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.Property(e => e.PaymentStatus)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValue("Pending");
        });

        modelBuilder.Entity<TblProduct>(entity =>
        {
            entity.HasKey(e => e.ProductId);

            entity.ToTable("tbl_Products");

            entity.Property(e => e.MetaDescription).HasMaxLength(2000);
            entity.Property(e => e.MetaKeyword).HasMaxLength(2000);
            entity.Property(e => e.Others).HasMaxLength(4000);
            entity.Property(e => e.ProductCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.ProductCreatedDate)
                .HasDefaultValueSql("(getdate())", "DF_tbl_Products_ProductCreatedDate")
                .HasColumnType("datetime");
            entity.Property(e => e.ProductDeployment).HasMaxLength(4000);
            entity.Property(e => e.ProductDocument)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.ProductIconClass)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ProductLargeImage)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.ProductName).HasMaxLength(500);
            entity.Property(e => e.ProductShortDesc).HasMaxLength(2000);
            entity.Property(e => e.ProductShortName).HasMaxLength(100);
            entity.Property(e => e.ProductSlogan).HasMaxLength(400);
            entity.Property(e => e.ProductSmallImage)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.ProductTags).HasMaxLength(500);
            entity.Property(e => e.Rewrite)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.TitleBrowser).HasMaxLength(500);
        });

        modelBuilder.Entity<TblQuotation>(entity =>
        {
            entity.HasKey(e => e.QuotationId).HasName("PK__tbl_Quot__E19752938F4FC112");

            entity.ToTable("tbl_Quotation");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18, 2)");

            entity.Property(e => e.QuatationStatus)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValue("Draft");
            entity.Property(e => e.QuotationDate).HasColumnType("datetime");
            entity.Property(e => e.QuotationNo)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TblQuotationDetail>(entity =>
        {
            entity.HasKey(e => e.QuotationDetailId).HasName("PK__tbl_Quot__0CEE6AE2EC238ED5");
            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 2)");

            entity.ToTable("tbl_QuotationDetail");
        });

        modelBuilder.Entity<TblService>(entity =>
        {
            entity.HasKey(e => e.ServiceId).HasName("PK__tbl_Serv__C51BB00A20C1E124");

            entity.ToTable("tbl_Services");

            entity.Property(e => e.DateCreated)
                .HasDefaultValueSql("(getdate())", "DF_tbl_Services_DateCreated")
                .HasColumnType("datetime");
            entity.Property(e => e.DateModified).HasColumnType("datetime");
            entity.Property(e => e.Ftpaccounts).HasColumnName("FTPAccounts");
            entity.Property(e => e.HasChild).HasDefaultValue(false, "DF_tbl_Services_HasChild");
            entity.Property(e => e.MetaDescription).HasMaxLength(2000);
            entity.Property(e => e.MetaKeyword).HasMaxLength(2000);
            entity.Property(e => e.MySql).HasColumnName("MySQL");
            entity.Property(e => e.Others).HasMaxLength(4000);
            entity.Property(e => e.Rewrite)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.ServiceImageIcon)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ServiceName).HasMaxLength(2000);
            entity.Property(e => e.ServiceRegion).HasComment("1. Tên miền Việt Nam; 2. Tên miền quốc tế");
            entity.Property(e => e.ServiceTypeId).HasComment("1 - Windows Hosting, 2 - Linux Hosting");
            entity.Property(e => e.Status).HasDefaultValue((byte)0, "DF_tbl_Services_Status");
            entity.Property(e => e.TitleBrowser).HasMaxLength(500);
        });

        modelBuilder.Entity<TblServiceType>(entity =>
        {
            entity.HasKey(e => e.ServiceTypeId);

            entity.ToTable("tbl_ServiceTypes");

            entity.Property(e => e.ServiceTypeId).ValueGeneratedOnAdd();
            entity.Property(e => e.ServiceTypeName).HasMaxLength(200);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
