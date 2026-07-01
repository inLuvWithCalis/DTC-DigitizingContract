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

    public virtual DbSet<TblApprovalHistory> TblApprovalHistories { get; set; }

    public virtual DbSet<TblApprovalWorkflow> TblApprovalWorkflows { get; set; }

    public virtual DbSet<TblCategory> TblCategories { get; set; }

    public virtual DbSet<TblContract> TblContracts { get; set; }

    public virtual DbSet<TblContractAppendix> TblContractAppendices { get; set; }

    public virtual DbSet<TblContractAttachment> TblContractAttachments { get; set; }

    public virtual DbSet<TblContractTerm> TblContractTerms { get; set; }

    public virtual DbSet<TblContractVersion> TblContractVersions { get; set; }

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

            entity.ToTable("tbl_Contract");

            entity.Property(e => e.ContractCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ContractName).HasMaxLength(1000);
            entity.Property(e => e.ContractNameEn)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())", "DF_tbl_Contract_CreatedDate")
                .HasColumnType("datetime");
            entity.Property(e => e.EffectiveDate).HasColumnType("datetime");
            entity.Property(e => e.ExpireDate).HasColumnType("datetime");
            entity.Property(e => e.SignDate).HasColumnType("datetime");
            entity.Property(e => e.UpdateDate).HasColumnType("datetime");
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
        });

        modelBuilder.Entity<TblContractTerm>(entity =>
        {
            entity.HasKey(e => e.TermId).HasName("PK__tbl_Cont__410A21A507DB2563");

            entity.ToTable("tbl_ContractTerm");

            entity.Property(e => e.DisplayOrder).HasDefaultValue(0);
            entity.Property(e => e.TermTitle).HasMaxLength(500);
        });

        modelBuilder.Entity<TblContractVersion>(entity =>
        {
            entity.HasKey(e => e.VersionId).HasName("PK__tbl_Cont__16C6400F510A7D28");

            entity.ToTable("tbl_ContractVersion");

            entity.Property(e => e.ChangeNote).HasMaxLength(2000);
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
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
