USE [db_dtctech]
GO
/****** Object:  Table [dbo].[tbl_Categories]    Script Date: 6/15/2026 11:10:58 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[tbl_Categories](
	[CategoryId] [tinyint] IDENTITY(1,1) NOT NULL,
	[CategoryName] [nvarchar](500) NULL,
	[CategoryShortDesc] [nvarchar](1000) NULL,
	[CategoryOrder] [tinyint] NULL,
	[CategoryParentId] [tinyint] NULL,
	[LangId] [int] NULL,
	[Image] [varchar](50) NULL,
 CONSTRAINT [PK_tbl_Categories] PRIMARY KEY CLUSTERED 
(
	[CategoryId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[tbl_Contract]    Script Date: 6/15/2026 11:10:58 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[tbl_Contract](
	[ContractId] [int] IDENTITY(1,1) NOT NULL,
	[CustomerId] [int] NULL,
	[EmployeeId] [int] NULL,
	[ContractCode] [varchar](50) NULL,
	[ContractName] [nvarchar](1000) NULL,
	[ContractNameEn] [varchar](500) NULL,
	[SignDate] [datetime] NULL,
	[EffectiveDate] [datetime] NULL,
	[ExpireDate] [datetime] NULL,
	[Status] [tinyint] NULL,
	[TotalAmount] [float] NULL,
	[CreatedEmployeeId] [int] NULL,
	[UpdatedEmployeeId] [int] NULL,
	[CreatedDate] [datetime] NULL,
	[UpdateDate] [datetime] NULL,
 CONSTRAINT [PK_tbl_Contract] PRIMARY KEY CLUSTERED 
(
	[ContractId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[tbl_Customers]    Script Date: 6/15/2026 11:10:58 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[tbl_Customers](
	[CustomerId] [int] IDENTITY(1,1) NOT NULL,
	[CustomerCode] [varchar](30) NULL,
	[CustomerAccount] [varchar](50) NULL,
	[CustomerPassword] [varchar](50) NULL,
	[CustomerFullName] [nvarchar](100) NULL,
	[CustomerCompany] [nvarchar](1000) NULL,
	[CustomerAddress] [nvarchar](2000) NULL,
	[CustomerEmail] [varchar](50) NULL,
	[CustomerMobile] [varchar](15) NULL,
	[CustomerPhone] [varchar](15) NULL,
	[CustomerFaxNumber] [varchar](15) NULL,
	[CustomerTaxCode] [varchar](30) NULL,
	[CustomerCity] [nvarchar](1000) NULL,
	[CustomerZipCode] [varchar](15) NULL,
	[CustomerCountry] [nvarchar](200) NULL,
	[UserCreated] [int] NULL,
	[UserModified] [int] NULL,
	[DateCreated] [datetime] NULL,
	[DateModified] [datetime] NULL,
	[Status] [tinyint] NULL,
	[CustomerPoints] [int] NULL,
	[CustomerCardId] [int] NULL,
	[CustomerRegion] [smallint] NULL,
	[CustomerComments] [nvarchar](2000) NULL,
	[CustomerImageIcon] [varchar](50) NULL,
	[CustomerBirthday] [datetime] NULL,
	[CustomerWebsite] [nvarchar](500) NULL,
	[CustomerSourceId] [int] NULL,
	[CustomerParentId] [int] NULL,
	[CustomerNotes] [nvarchar](2000) NULL,
	[CustomerCareerId] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[CustomerId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[tbl_Department]    Script Date: 6/15/2026 11:10:58 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[tbl_Department](
	[DepartmentID] [smallint] IDENTITY(1,1) NOT NULL,
	[DepartmentCode] [varchar](20) NOT NULL,
	[DepartmentName] [nvarchar](200) NOT NULL,
	[ModifiedDate] [datetime] NULL,
	[Stutus] [tinyint] NULL,
	[LangId] [int] NULL,
 CONSTRAINT [PK_Department_DepartmentID] PRIMARY KEY CLUSTERED 
(
	[DepartmentID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[tbl_Employee]    Script Date: 6/15/2026 11:10:58 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[tbl_Employee](
	[EmployeeId] [int] IDENTITY(1,1) NOT NULL,
	[EmployeeCode] [varchar](30) NULL,
	[EmployeeAccount] [varchar](50) NULL,
	[EmployeePassword] [varchar](50) NULL,
	[EmployeeFullName] [nvarchar](100) NULL,
	[TitleId] [smallint] NULL,
	[EmployeeBirthDate] [datetime] NULL,
	[MaritalStatus] [nchar](1) NULL,
	[Gender] [nchar](1) NULL,
	[EmployeeMobile] [varchar](15) NULL,
	[EmployeePhone] [varchar](15) NULL,
	[EmployeeEmail] [varchar](100) NULL,
	[EmployeeAddress] [nvarchar](500) NULL,
	[UserCreated] [int] NULL,
	[UserModified] [int] NULL,
	[DateCreated] [datetime] NULL,
	[DateModified] [datetime] NULL,
	[HireDate] [datetime] NULL,
	[Status] [tinyint] NULL,
	[DepartmentId] [int] NULL,
	[Others] [nvarchar](2000) NULL,
	[DefaultPage] [varchar](200) NULL,
	[EmployeeImageIcon] [varchar](50) NULL,
	[EmployeeType] [tinyint] NULL,
	[UserRoles] [varchar](20) NULL,
	[WorkTypeId] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[EmployeeId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[tbl_OrderDetails]    Script Date: 6/15/2026 11:10:58 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[tbl_OrderDetails](
	[OrderDetailsId] [int] IDENTITY(1,1) NOT NULL,
	[OrderId] [int] NULL,
	[ProductId] [int] NULL,
	[OrderQuantity] [float] NULL,
	[UnitPrice] [float] NULL,
	[DateCreated] [datetime] NULL,
	[DateExpired] [datetime] NULL,
	[Status] [tinyint] NULL,
	[ItemType] [tinyint] NULL,
	[StartDate] [datetime] NULL,
	[NameDetails] [nvarchar](2000) NULL,
	[ItemGroupId] [smallint] NULL,
	[DiscountPercent] [float] NULL,
 CONSTRAINT [PK_tbl_OrderDetails] PRIMARY KEY CLUSTERED 
(
	[OrderDetailsId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[tbl_Orders]    Script Date: 6/15/2026 11:10:58 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[tbl_Orders](
	[OrderId] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [int] NULL,
	[ContractId] [int] NULL,
	[DateCreated] [datetime] NULL,
	[DateExpired] [datetime] NULL,
	[OrderStatus] [tinyint] NULL,
	[OrderType] [tinyint] NULL,
	[OrderComment] [nvarchar](4000) NULL,
	[NoteFromAdmin] [nvarchar](1000) NULL,
	[UpdatedUser] [int] NULL,
	[UpdatedDate] [datetime] NULL,
 CONSTRAINT [PK_tbl_Orders] PRIMARY KEY CLUSTERED 
(
	[OrderId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[tbl_Products]    Script Date: 6/15/2026 11:10:58 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[tbl_Products](
	[ProductId] [int] IDENTITY(1,1) NOT NULL,
	[ProductCode] [varchar](20) NULL,
	[ProductName] [nvarchar](500) NULL,
	[CategoryId] [int] NULL,
	[ProductShortDesc] [nvarchar](2000) NULL,
	[ProductDetails] [nvarchar](max) NULL,
	[ProductFeatures] [nvarchar](max) NULL,
	[ProductDeployment] [nvarchar](4000) NULL,
	[ProductBenefit] [nvarchar](max) NULL,
	[ProductSmallImage] [varchar](500) NULL,
	[ProductLargeImage] [varchar](500) NULL,
	[ProductPrice] [float] NULL,
	[LangId] [tinyint] NULL,
	[Status] [tinyint] NULL,
	[Others] [nvarchar](4000) NULL,
	[ViewTotal] [int] NULL,
	[Customers] [nvarchar](max) NULL,
	[ProductOverall] [nvarchar](max) NULL,
	[ProductIconClass] [varchar](50) NULL,
	[ProductSlogan] [nvarchar](400) NULL,
	[DisplaySlide] [tinyint] NULL,
	[GetTrialVersion] [bit] NULL,
	[ProductOrder] [int] NULL,
	[ProductShortName] [nvarchar](100) NULL,
	[MetaKeyword] [nvarchar](2000) NULL,
	[MetaDescription] [nvarchar](2000) NULL,
	[ProductTags] [nvarchar](500) NULL,
	[Rewrite] [varchar](200) NULL,
	[TitleBrowser] [nvarchar](500) NULL,
	[ProductDocument] [varchar](200) NULL,
	[ProductCreatedDate] [datetime] NULL,
	[GoogleClick] [int] NULL,
 CONSTRAINT [PK_tbl_Products] PRIMARY KEY CLUSTERED 
(
	[ProductId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[tbl_Services]    Script Date: 6/15/2026 11:10:58 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[tbl_Services](
	[ServiceId] [int] IDENTITY(1,1) NOT NULL,
	[ServiceName] [nvarchar](2000) NULL,
	[ServiceParentId] [int] NULL,
	[Points] [int] NULL,
	[UserCreated] [int] NULL,
	[UserModified] [int] NULL,
	[DateCreated] [datetime] NULL,
	[DateModified] [datetime] NULL,
	[Status] [tinyint] NULL,
	[ServicePrice] [float] NULL,
	[DiskStorage] [int] NULL,
	[Bandwidth] [int] NULL,
	[SubDomain] [smallint] NULL,
	[EmailAccounts] [smallint] NULL,
	[FTPAccounts] [tinyint] NULL,
	[MySQL] [tinyint] NULL,
	[MsSqlServer] [tinyint] NULL,
	[ParkDomain] [int] NULL,
	[ServiceTypeId] [tinyint] NULL,
	[ServicePackageId] [tinyint] NULL,
	[LangId] [int] NULL,
	[ServiceImageIcon] [varchar](50) NULL,
	[ServiceGroupId] [smallint] NULL,
	[SetupPrice] [float] NULL,
	[MaintainPrice] [float] NULL,
	[HasChild] [bit] NULL,
	[ServiceContent] [nvarchar](max) NULL,
	[ServiceOrder] [int] NULL,
	[ServiceRegion] [tinyint] NULL,
	[ServiceShortDesc] [nvarchar](max) NULL,
	[EmailForwarders] [int] NULL,
	[Others] [nvarchar](4000) NULL,
	[Rewrite] [varchar](300) NULL,
	[MetaKeyword] [nvarchar](2000) NULL,
	[MetaDescription] [nvarchar](2000) NULL,
	[TitleBrowser] [nvarchar](500) NULL,
 CONSTRAINT [PK__tbl_Serv__C51BB00A20C1E124] PRIMARY KEY CLUSTERED 
(
	[ServiceId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[tbl_ServiceTypes]    Script Date: 6/15/2026 11:10:58 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[tbl_ServiceTypes](
	[ServiceTypeId] [tinyint] IDENTITY(1,1) NOT NULL,
	[ServiceTypeName] [nvarchar](200) NULL,
	[LangId] [tinyint] NULL,
 CONSTRAINT [PK_tbl_ServiceTypes] PRIMARY KEY CLUSTERED 
(
	[ServiceTypeId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[tbl_Contract] ADD  CONSTRAINT [DF_tbl_Contract_CreatedDate]  DEFAULT (getdate()) FOR [CreatedDate]
GO
ALTER TABLE [dbo].[tbl_Customers] ADD  CONSTRAINT [DF_tbl_Customers_DateCreated]  DEFAULT (getdate()) FOR [DateCreated]
GO
ALTER TABLE [dbo].[tbl_Customers] ADD  CONSTRAINT [DF_tbl_Customers_Status]  DEFAULT ((0)) FOR [Status]
GO
ALTER TABLE [dbo].[tbl_OrderDetails] ADD  CONSTRAINT [DF_tbl_OrderDetails_DateCreated]  DEFAULT (getdate()) FOR [DateCreated]
GO
ALTER TABLE [dbo].[tbl_OrderDetails] ADD  CONSTRAINT [DF_tbl_OrderDetails_Status]  DEFAULT ((0)) FOR [Status]
GO
ALTER TABLE [dbo].[tbl_Orders] ADD  CONSTRAINT [DF_tbl_Orders_DateCreated]  DEFAULT (getdate()) FOR [DateCreated]
GO
ALTER TABLE [dbo].[tbl_Orders] ADD  CONSTRAINT [DF_tbl_Orders_OrderStatus]  DEFAULT ((0)) FOR [OrderStatus]
GO
ALTER TABLE [dbo].[tbl_Products] ADD  CONSTRAINT [DF_tbl_Products_ProductCreatedDate]  DEFAULT (getdate()) FOR [ProductCreatedDate]
GO
ALTER TABLE [dbo].[tbl_Services] ADD  CONSTRAINT [DF_tbl_Services_DateCreated]  DEFAULT (getdate()) FOR [DateCreated]
GO
ALTER TABLE [dbo].[tbl_Services] ADD  CONSTRAINT [DF_tbl_Services_Status]  DEFAULT ((0)) FOR [Status]
GO
ALTER TABLE [dbo].[tbl_Services] ADD  CONSTRAINT [DF_tbl_Services_HasChild]  DEFAULT ((0)) FOR [HasChild]
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'0: Pending, 1: Approved' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'tbl_Orders', @level2type=N'COLUMN',@level2name=N'OrderStatus'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'1 - Windows Hosting, 2 - Linux Hosting' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'tbl_Services', @level2type=N'COLUMN',@level2name=N'ServiceTypeId'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'1. Tên miền Việt Nam; 2. Tên miền quốc tế' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'tbl_Services', @level2type=N'COLUMN',@level2name=N'ServiceRegion'
GO
CREATE TABLE tbl_ContractVersion
(
    VersionId INT IDENTITY(1,1) PRIMARY KEY, 
    ContractId INT NOT NULL, 
    VersionNo INT NOT NULL, 
    ChangeNote NVARCHAR(2000), 
    CreatedDate DATETIME NOT NULL DEFAULT(GETDATE()),

    
);
CREATE TABLE tbl_ContractAppendix
(
    AppendixId INT IDENTITY(1,1) PRIMARY KEY, 
    ContractId INT NOT NULL, 
    AppendixCode VARCHAR(50),
    AppendixName NVARCHAR(1000), 
    AppendixNameEn VARCHAR(500),
    AppendixDate DATETIME, 
    AppendixDescription NVARCHAR(2000),

    
);
CREATE TABLE tbl_ContractAttachment
(
    AttachmentId INT IDENTITY(1,1) PRIMARY KEY, 
    ContractId INT NOT NULL, 
    ContractFileName VARCHAR(300),
    ContractFilePath VARCHAR(1000), 
    UploadDate DATETIME DEFAULT(GETDATE()),
    UploadEmployeeId int
 
);
CREATE TABLE tbl_ContractTerm
(
    TermId INT IDENTITY(1,1) PRIMARY KEY,
    ContractId INT NOT NULL,
    TermTitle NVARCHAR(500) NOT NULL,
    TermContent NVARCHAR(MAX),
    DisplayOrder INT DEFAULT(0) 
);

CREATE TABLE tbl_Invoice
(
    InvoiceId INT IDENTITY(1,1) PRIMARY KEY, 
    OrderId INT NOT NULL,
    ContractId INT NULL, 
    InvoiceNo VARCHAR(50) NOT NULL, 
    InvoiceDate DATETIME NOT NULL, 
    TotalAmount FLOAT  NOT NULL, 
    InvoiceStatus VARCHAR(30) DEFAULT('Unpaid')   
);
CREATE TABLE tbl_Payment
(
    PaymentId INT IDENTITY(1,1) PRIMARY KEY, 
    InvoiceId INT NOT NULL, 
    PaymentDate DATETIME NOT NULL, 
    Amount FLOAT NOT NULL, 
    PaymentMethod VARCHAR(50), 
    ReferenceNo VARCHAR(100) 
);
CREATE TABLE tbl_PaymentSchedule
(
    ScheduleId INT IDENTITY(1,1) PRIMARY KEY,
    ContractId INT NOT NULL,
    DueDate DATETIME NOT NULL,
    Amount FLOAT NOT NULL,
    PaidAmount FLOAT NOT NULL DEFAULT(0),
    PaymentStatus VARCHAR(30) NOT NULL DEFAULT('Pending'),
    Note NVARCHAR(500) 
);
CREATE TABLE tbl_Quotation
(
    QuotationId INT IDENTITY(1,1) PRIMARY KEY, 
    CustomerId INT NOT NULL, 
    QuotationNo VARCHAR(50) NOT NULL, 
    QuotationDate DATETIME NOT NULL, 
    TotalAmount FLOAT NOT NULL, 
    QuatationStatus VARCHAR(30) DEFAULT('Draft'),
    CreatedEmployeeId int
);
CREATE TABLE tbl_QuotationDetail
(
    QuotationDetailId INT IDENTITY(1,1) PRIMARY KEY, 
    QuotationId INT NOT NULL,
    ProductId INT NOT NULL, 
    Quantity INT,
    UnitPrice FLOAT, 
    Amount FLOAT 
);
CREATE TABLE tbl_DeliveryOrder
(
    DeliveryId INT IDENTITY(1,1) PRIMARY KEY,
    OrderId INT NOT NULL,
    DeliveryNo VARCHAR(50) NOT NULL,
    DeliveryDate DATETIME NOT NULL,
    DeliveryAddress NVARCHAR(1000),
    ReceiverName NVARCHAR(200),
    ReceiverPhone VARCHAR(20),
    DeliveryStatus VARCHAR(30) NOT NULL DEFAULT('Pending') 
);
CREATE TABLE tbl_DeliveryDetail
(
    DeliveryDetailId INT IDENTITY(1,1) PRIMARY KEY,
    DeliveryId INT NOT NULL,
    ProductId INT NOT NULL,
    Quantity int NOT NULL,
    Note NVARCHAR(1000) 
);
CREATE TABLE tbl_ApprovalWorkflow
(
    WorkflowId INT IDENTITY(1,1) PRIMARY KEY,
    WorkflowName NVARCHAR(200) NOT NULL,
    ObjectType VARCHAR(50) NOT NULL, -- Contract, Quotation, SalesOrder, Invoice
    StepNo INT NOT NULL,
    ApproverRoleId INT NULL,
    ApproverEmployeeId INT NULL,
    IsActive BIT NOT NULL DEFAULT(1) 
);
CREATE TABLE tbl_ApprovalHistory
(
    ApprovalHistoryId INT IDENTITY(1,1) PRIMARY KEY,
    WorkflowId INT NULL,
    ObjectType VARCHAR(50) NOT NULL,
    ObjectId INT NOT NULL,
    StepNo INT NOT NULL,
    ApproverEmployeeId INT NOT NULL,
    ApprovalAction VARCHAR(30) NOT NULL, -- Approved, Rejected, Returned
    Comment NVARCHAR(1000),
    ActionDate DATETIME NOT NULL DEFAULT(GETDATE()) 
);
CREATE TABLE tbl_CustomerInteraction
(
    InteractionId INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId INT NOT NULL,
    EmployeeId INT NOT NULL,
    InteractionDate DATETIME NOT NULL DEFAULT(GETDATE()),
    InteractionType VARCHAR(50) NOT NULL, -- Call, Email, Meeting, Zalo
    InteractionSubject NVARCHAR(1000),
    Content NVARCHAR(MAX),
    NextFollowUpDate DATETIME 
);
CREATE TABLE tbl_Notification
(
    NotificationId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    NotificationTitle NVARCHAR(300) NOT NULL,
    NotificationMessage NVARCHAR(MAX),
    ObjectType VARCHAR(50),
    ObjectId INT,
    IsRead BIT NOT NULL DEFAULT(0),
    CreatedDate DATETIME NOT NULL DEFAULT(GETDATE()),
     
);
CREATE TABLE tbl_FileStorage
(
    FileId INT IDENTITY(1,1) PRIMARY KEY,
    ObjectType VARCHAR(50) NOT NULL, -- Contract, Customer, Invoice, Delivery
    ObjectId INT NOT NULL,
    FileName NVARCHAR(300) NOT NULL,
    FilePath NVARCHAR(1000) NOT NULL,
    FileType VARCHAR(100),
    FileSize BIGINT,
    UploadedByUserId INT NULL,
    UploadedDate DATETIME NOT NULL DEFAULT(GETDATE()) 
);