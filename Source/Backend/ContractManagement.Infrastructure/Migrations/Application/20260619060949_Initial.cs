using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractManagement.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tbl_ApprovalHistory",
                columns: table => new
                {
                    ApprovalHistoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkflowId = table.Column<int>(type: "int", nullable: true),
                    ObjectType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    ObjectId = table.Column<int>(type: "int", nullable: false),
                    StepNo = table.Column<int>(type: "int", nullable: false),
                    ApproverEmployeeId = table.Column<int>(type: "int", nullable: false),
                    ApprovalAction = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ActionDate = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__tbl_Appr__46B53247FB4F4CF5", x => x.ApprovalHistoryId);
                });

            migrationBuilder.CreateTable(
                name: "tbl_ApprovalWorkflow",
                columns: table => new
                {
                    WorkflowId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkflowName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ObjectType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    StepNo = table.Column<int>(type: "int", nullable: false),
                    ApproverRoleId = table.Column<int>(type: "int", nullable: true),
                    ApproverEmployeeId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__tbl_Appr__5704A66A2F3CD2A5", x => x.WorkflowId);
                });

            migrationBuilder.CreateTable(
                name: "tbl_Categories",
                columns: table => new
                {
                    CategoryId = table.Column<byte>(type: "tinyint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CategoryShortDesc = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CategoryOrder = table.Column<byte>(type: "tinyint", nullable: true),
                    CategoryParentId = table.Column<byte>(type: "tinyint", nullable: true),
                    LangId = table.Column<int>(type: "int", nullable: true),
                    Image = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_Categories", x => x.CategoryId);
                });

            migrationBuilder.CreateTable(
                name: "tbl_Contract",
                columns: table => new
                {
                    ContractId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    ContractCode = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    ContractName = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ContractNameEn = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    SignDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    EffectiveDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    ExpireDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: true),
                    TotalAmount = table.Column<double>(type: "float", nullable: true),
                    CreatedEmployeeId = table.Column<int>(type: "int", nullable: true),
                    UpdatedEmployeeId = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                        .Annotation("Relational:DefaultConstraintName", "DF_tbl_Contract_CreatedDate"),
                    UpdateDate = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_Contract", x => x.ContractId);
                });

            migrationBuilder.CreateTable(
                name: "tbl_ContractAppendix",
                columns: table => new
                {
                    AppendixId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractId = table.Column<int>(type: "int", nullable: false),
                    AppendixCode = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    AppendixName = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AppendixNameEn = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    AppendixDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    AppendixDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__tbl_Cont__44B149C44BC57074", x => x.AppendixId);
                });

            migrationBuilder.CreateTable(
                name: "tbl_ContractAttachment",
                columns: table => new
                {
                    AttachmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractId = table.Column<int>(type: "int", nullable: false),
                    ContractFileName = table.Column<string>(type: "varchar(300)", unicode: false, maxLength: 300, nullable: true),
                    ContractFilePath = table.Column<string>(type: "varchar(1000)", unicode: false, maxLength: 1000, nullable: true),
                    UploadDate = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    UploadEmployeeId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__tbl_Cont__442C64BE6AC2F4CE", x => x.AttachmentId);
                });

            migrationBuilder.CreateTable(
                name: "tbl_ContractTerm",
                columns: table => new
                {
                    TermId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractId = table.Column<int>(type: "int", nullable: false),
                    TermTitle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TermContent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: true, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__tbl_Cont__410A21A507DB2563", x => x.TermId);
                });

            migrationBuilder.CreateTable(
                name: "tbl_ContractVersion",
                columns: table => new
                {
                    VersionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractId = table.Column<int>(type: "int", nullable: false),
                    VersionNo = table.Column<int>(type: "int", nullable: false),
                    ChangeNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__tbl_Cont__16C6400F510A7D28", x => x.VersionId);
                });

            migrationBuilder.CreateTable(
                name: "tbl_CustomerInteraction",
                columns: table => new
                {
                    InteractionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    InteractionDate = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    InteractionType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    InteractionSubject = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NextFollowUpDate = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__tbl_Cust__922C0496FCECB578", x => x.InteractionId);
                });

            migrationBuilder.CreateTable(
                name: "tbl_Customers",
                columns: table => new
                {
                    CustomerId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerCode = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    CustomerAccount = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    CustomerPassword = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    CustomerFullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CustomerCompany = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CustomerAddress = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CustomerEmail = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    CustomerMobile = table.Column<string>(type: "varchar(15)", unicode: false, maxLength: 15, nullable: true),
                    CustomerPhone = table.Column<string>(type: "varchar(15)", unicode: false, maxLength: 15, nullable: true),
                    CustomerFaxNumber = table.Column<string>(type: "varchar(15)", unicode: false, maxLength: 15, nullable: true),
                    CustomerTaxCode = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    CustomerCity = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CustomerZipCode = table.Column<string>(type: "varchar(15)", unicode: false, maxLength: 15, nullable: true),
                    CustomerCountry = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UserCreated = table.Column<int>(type: "int", nullable: true),
                    UserModified = table.Column<int>(type: "int", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                        .Annotation("Relational:DefaultConstraintName", "DF_tbl_Customers_DateCreated"),
                    DateModified = table.Column<DateTime>(type: "datetime", nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: true, defaultValue: (byte)0)
                        .Annotation("Relational:DefaultConstraintName", "DF_tbl_Customers_Status"),
                    CustomerPoints = table.Column<int>(type: "int", nullable: true),
                    CustomerCardId = table.Column<int>(type: "int", nullable: true),
                    CustomerRegion = table.Column<short>(type: "smallint", nullable: true),
                    CustomerComments = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CustomerImageIcon = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    CustomerBirthday = table.Column<DateTime>(type: "datetime", nullable: true),
                    CustomerWebsite = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CustomerSourceId = table.Column<int>(type: "int", nullable: true),
                    CustomerParentId = table.Column<int>(type: "int", nullable: true),
                    CustomerNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CustomerCareerId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__tbl_Cust__A4AE64D84BC5AE01", x => x.CustomerId);
                });

            migrationBuilder.CreateTable(
                name: "tbl_DeliveryDetail",
                columns: table => new
                {
                    DeliveryDetailId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeliveryId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__tbl_Deli__EFD2C2E7914C480F", x => x.DeliveryDetailId);
                });

            migrationBuilder.CreateTable(
                name: "tbl_DeliveryOrder",
                columns: table => new
                {
                    DeliveryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    DeliveryNo = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    DeliveryDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    DeliveryAddress = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ReceiverName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ReceiverPhone = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    DeliveryStatus = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false, defaultValue: "Pending")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__tbl_Deli__626D8FCEF0133037", x => x.DeliveryId);
                });

            migrationBuilder.CreateTable(
                name: "tbl_Department",
                columns: table => new
                {
                    DepartmentID = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DepartmentCode = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    DepartmentName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    Stutus = table.Column<byte>(type: "tinyint", nullable: true),
                    LangId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Department_DepartmentID", x => x.DepartmentID);
                });

            migrationBuilder.CreateTable(
                name: "tbl_Employee",
                columns: table => new
                {
                    EmployeeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeCode = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    EmployeeAccount = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    EmployeePassword = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    EmployeeFullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TitleId = table.Column<short>(type: "smallint", nullable: true),
                    EmployeeBirthDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    MaritalStatus = table.Column<string>(type: "nchar(1)", fixedLength: true, maxLength: 1, nullable: true),
                    Gender = table.Column<string>(type: "nchar(1)", fixedLength: true, maxLength: 1, nullable: true),
                    EmployeeMobile = table.Column<string>(type: "varchar(15)", unicode: false, maxLength: 15, nullable: true),
                    EmployeePhone = table.Column<string>(type: "varchar(15)", unicode: false, maxLength: 15, nullable: true),
                    EmployeeEmail = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    EmployeeAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UserCreated = table.Column<int>(type: "int", nullable: true),
                    UserModified = table.Column<int>(type: "int", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime", nullable: true),
                    HireDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: true),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    Others = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DefaultPage = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    EmployeeImageIcon = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    EmployeeType = table.Column<byte>(type: "tinyint", nullable: true),
                    UserRoles = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    WorkTypeId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__tbl_Empl__7AD04F11C415E4E3", x => x.EmployeeId);
                });

            migrationBuilder.CreateTable(
                name: "tbl_FileStorage",
                columns: table => new
                {
                    FileId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ObjectType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    ObjectId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    FileType = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    FileSize = table.Column<long>(type: "bigint", nullable: true),
                    UploadedByUserId = table.Column<int>(type: "int", nullable: true),
                    UploadedDate = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__tbl_File__6F0F98BF777CA8CB", x => x.FileId);
                });

            migrationBuilder.CreateTable(
                name: "tbl_Invoice",
                columns: table => new
                {
                    InvoiceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    ContractId = table.Column<int>(type: "int", nullable: true),
                    InvoiceNo = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    InvoiceDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    TotalAmount = table.Column<double>(type: "float", nullable: false),
                    InvoiceStatus = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true, defaultValue: "Unpaid")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__tbl_Invo__D796AAB5EDC7461D", x => x.InvoiceId);
                });

            migrationBuilder.CreateTable(
                name: "tbl_Notification",
                columns: table => new
                {
                    NotificationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    NotificationTitle = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    NotificationMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ObjectType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    ObjectId = table.Column<int>(type: "int", nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__tbl_Noti__20CF2E127E71675E", x => x.NotificationId);
                });

            migrationBuilder.CreateTable(
                name: "tbl_OrderDetails",
                columns: table => new
                {
                    OrderDetailsId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: true),
                    ProductId = table.Column<int>(type: "int", nullable: true),
                    OrderQuantity = table.Column<double>(type: "float", nullable: true),
                    UnitPrice = table.Column<double>(type: "float", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                        .Annotation("Relational:DefaultConstraintName", "DF_tbl_OrderDetails_DateCreated"),
                    DateExpired = table.Column<DateTime>(type: "datetime", nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: true, defaultValue: (byte)0)
                        .Annotation("Relational:DefaultConstraintName", "DF_tbl_OrderDetails_Status"),
                    ItemType = table.Column<byte>(type: "tinyint", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    NameDetails = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ItemGroupId = table.Column<short>(type: "smallint", nullable: true),
                    DiscountPercent = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_OrderDetails", x => x.OrderDetailsId);
                });

            migrationBuilder.CreateTable(
                name: "tbl_Orders",
                columns: table => new
                {
                    OrderId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    ContractId = table.Column<int>(type: "int", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                        .Annotation("Relational:DefaultConstraintName", "DF_tbl_Orders_DateCreated"),
                    DateExpired = table.Column<DateTime>(type: "datetime", nullable: true),
                    OrderStatus = table.Column<byte>(type: "tinyint", nullable: true, defaultValue: (byte)0, comment: "0: Pending, 1: Approved")
                        .Annotation("Relational:DefaultConstraintName", "DF_tbl_Orders_OrderStatus"),
                    OrderType = table.Column<byte>(type: "tinyint", nullable: true),
                    OrderComment = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    NoteFromAdmin = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    UpdatedUser = table.Column<int>(type: "int", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_Orders", x => x.OrderId);
                });

            migrationBuilder.CreateTable(
                name: "tbl_Payment",
                columns: table => new
                {
                    PaymentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceId = table.Column<int>(type: "int", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    Amount = table.Column<double>(type: "float", nullable: false),
                    PaymentMethod = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    ReferenceNo = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__tbl_Paym__9B556A380D56A7E2", x => x.PaymentId);
                });

            migrationBuilder.CreateTable(
                name: "tbl_PaymentSchedule",
                columns: table => new
                {
                    ScheduleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractId = table.Column<int>(type: "int", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    Amount = table.Column<double>(type: "float", nullable: false),
                    PaidAmount = table.Column<double>(type: "float", nullable: false),
                    PaymentStatus = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false, defaultValue: "Pending"),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__tbl_Paym__9C8A5B49EE0F0668", x => x.ScheduleId);
                });

            migrationBuilder.CreateTable(
                name: "tbl_Products",
                columns: table => new
                {
                    ProductId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductCode = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    ProductName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CategoryId = table.Column<int>(type: "int", nullable: true),
                    ProductShortDesc = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ProductDetails = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProductFeatures = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProductDeployment = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ProductBenefit = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProductSmallImage = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    ProductLargeImage = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    ProductPrice = table.Column<double>(type: "float", nullable: true),
                    LangId = table.Column<byte>(type: "tinyint", nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: true),
                    Others = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ViewTotal = table.Column<int>(type: "int", nullable: true),
                    Customers = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProductOverall = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProductIconClass = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    ProductSlogan = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    DisplaySlide = table.Column<byte>(type: "tinyint", nullable: true),
                    GetTrialVersion = table.Column<bool>(type: "bit", nullable: true),
                    ProductOrder = table.Column<int>(type: "int", nullable: true),
                    ProductShortName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MetaKeyword = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    MetaDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ProductTags = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Rewrite = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    TitleBrowser = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ProductDocument = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    ProductCreatedDate = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                        .Annotation("Relational:DefaultConstraintName", "DF_tbl_Products_ProductCreatedDate"),
                    GoogleClick = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_Products", x => x.ProductId);
                });

            migrationBuilder.CreateTable(
                name: "tbl_Quotation",
                columns: table => new
                {
                    QuotationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    QuotationNo = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    QuotationDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    TotalAmount = table.Column<double>(type: "float", nullable: false),
                    QuatationStatus = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true, defaultValue: "Draft"),
                    CreatedEmployeeId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__tbl_Quot__E19752938F4FC112", x => x.QuotationId);
                });

            migrationBuilder.CreateTable(
                name: "tbl_QuotationDetail",
                columns: table => new
                {
                    QuotationDetailId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuotationId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: true),
                    UnitPrice = table.Column<double>(type: "float", nullable: true),
                    Amount = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__tbl_Quot__0CEE6AE2EC238ED5", x => x.QuotationDetailId);
                });

            migrationBuilder.CreateTable(
                name: "tbl_Services",
                columns: table => new
                {
                    ServiceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceName = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ServiceParentId = table.Column<int>(type: "int", nullable: true),
                    Points = table.Column<int>(type: "int", nullable: true),
                    UserCreated = table.Column<int>(type: "int", nullable: true),
                    UserModified = table.Column<int>(type: "int", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                        .Annotation("Relational:DefaultConstraintName", "DF_tbl_Services_DateCreated"),
                    DateModified = table.Column<DateTime>(type: "datetime", nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: true, defaultValue: (byte)0)
                        .Annotation("Relational:DefaultConstraintName", "DF_tbl_Services_Status"),
                    ServicePrice = table.Column<double>(type: "float", nullable: true),
                    DiskStorage = table.Column<int>(type: "int", nullable: true),
                    Bandwidth = table.Column<int>(type: "int", nullable: true),
                    SubDomain = table.Column<short>(type: "smallint", nullable: true),
                    EmailAccounts = table.Column<short>(type: "smallint", nullable: true),
                    FTPAccounts = table.Column<byte>(type: "tinyint", nullable: true),
                    MySQL = table.Column<byte>(type: "tinyint", nullable: true),
                    MsSqlServer = table.Column<byte>(type: "tinyint", nullable: true),
                    ParkDomain = table.Column<int>(type: "int", nullable: true),
                    ServiceTypeId = table.Column<byte>(type: "tinyint", nullable: true, comment: "1 - Windows Hosting, 2 - Linux Hosting"),
                    ServicePackageId = table.Column<byte>(type: "tinyint", nullable: true),
                    LangId = table.Column<int>(type: "int", nullable: true),
                    ServiceImageIcon = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    ServiceGroupId = table.Column<short>(type: "smallint", nullable: true),
                    SetupPrice = table.Column<double>(type: "float", nullable: true),
                    MaintainPrice = table.Column<double>(type: "float", nullable: true),
                    HasChild = table.Column<bool>(type: "bit", nullable: true, defaultValue: false)
                        .Annotation("Relational:DefaultConstraintName", "DF_tbl_Services_HasChild"),
                    ServiceContent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ServiceOrder = table.Column<int>(type: "int", nullable: true),
                    ServiceRegion = table.Column<byte>(type: "tinyint", nullable: true, comment: "1. Tên miền Việt Nam; 2. Tên miền quốc tế"),
                    ServiceShortDesc = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmailForwarders = table.Column<int>(type: "int", nullable: true),
                    Others = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Rewrite = table.Column<string>(type: "varchar(300)", unicode: false, maxLength: 300, nullable: true),
                    MetaKeyword = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    MetaDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TitleBrowser = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__tbl_Serv__C51BB00A20C1E124", x => x.ServiceId);
                });

            migrationBuilder.CreateTable(
                name: "tbl_ServiceTypes",
                columns: table => new
                {
                    ServiceTypeId = table.Column<byte>(type: "tinyint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceTypeName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LangId = table.Column<byte>(type: "tinyint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_ServiceTypes", x => x.ServiceTypeId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbl_ApprovalHistory");

            migrationBuilder.DropTable(
                name: "tbl_ApprovalWorkflow");

            migrationBuilder.DropTable(
                name: "tbl_Categories");

            migrationBuilder.DropTable(
                name: "tbl_Contract");

            migrationBuilder.DropTable(
                name: "tbl_ContractAppendix");

            migrationBuilder.DropTable(
                name: "tbl_ContractAttachment");

            migrationBuilder.DropTable(
                name: "tbl_ContractTerm");

            migrationBuilder.DropTable(
                name: "tbl_ContractVersion");

            migrationBuilder.DropTable(
                name: "tbl_CustomerInteraction");

            migrationBuilder.DropTable(
                name: "tbl_Customers");

            migrationBuilder.DropTable(
                name: "tbl_DeliveryDetail");

            migrationBuilder.DropTable(
                name: "tbl_DeliveryOrder");

            migrationBuilder.DropTable(
                name: "tbl_Department");

            migrationBuilder.DropTable(
                name: "tbl_Employee");

            migrationBuilder.DropTable(
                name: "tbl_FileStorage");

            migrationBuilder.DropTable(
                name: "tbl_Invoice");

            migrationBuilder.DropTable(
                name: "tbl_Notification");

            migrationBuilder.DropTable(
                name: "tbl_OrderDetails");

            migrationBuilder.DropTable(
                name: "tbl_Orders");

            migrationBuilder.DropTable(
                name: "tbl_Payment");

            migrationBuilder.DropTable(
                name: "tbl_PaymentSchedule");

            migrationBuilder.DropTable(
                name: "tbl_Products");

            migrationBuilder.DropTable(
                name: "tbl_Quotation");

            migrationBuilder.DropTable(
                name: "tbl_QuotationDetail");

            migrationBuilder.DropTable(
                name: "tbl_Services");

            migrationBuilder.DropTable(
                name: "tbl_ServiceTypes");
        }
    }
}
