/*
  DEV/TEST ONLY

  Seed dữ liệu catalog cho tenant hungnd theo entity/service hiện tại:
    - 10 danh mục (tbl_Categories), gồm danh mục cha và con.
    - 10 loại dịch vụ (tbl_ServiceTypes).
    - 10 sản phẩm active (tbl_Products), liên kết danh mục bằng CategoryId.
    - 10 dịch vụ active (tbl_Services), liên kết loại dịch vụ bằng ServiceTypeId.

  Script có thể chạy lại:
    - Danh mục được nhận diện bằng CategoryName.
    - Loại dịch vụ được nhận diện bằng ServiceTypeName.
    - Sản phẩm được nhận diện bằng ProductCode.
    - Dịch vụ được nhận diện bằng ServiceName.

  Các cột identity không được insert trực tiếp.
*/

USE [ContractManagement_Tenant_hungnd];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @LangId tinyint = 1;
DECLARE @CreatedByEmployeeId int;

IF DB_NAME() <> 'ContractManagement_Tenant_hungnd'
BEGIN
    THROW 50020, N'Sai database đích. Hãy kiểm tra lại câu lệnh USE.', 1;
END;

-- Ưu tiên tài khoản admin; fallback sang một Manager active.
SELECT TOP (1)
    @CreatedByEmployeeId = EmployeeId
FROM dbo.tbl_Employee
WHERE EmployeeAccount = 'admin'
ORDER BY EmployeeId;

IF @CreatedByEmployeeId IS NULL
BEGIN
    SELECT TOP (1)
        @CreatedByEmployeeId = EmployeeId
    FROM dbo.tbl_Employee
    WHERE EmployeeType = 6
      AND Status = 1
    ORDER BY EmployeeId;
END;

IF @CreatedByEmployeeId IS NULL
BEGIN
    THROW 50021, N'Không tìm thấy admin hoặc Manager active để gán UserCreated cho dịch vụ.', 1;
END;

DECLARE @CategorySeed table
(
    CategoryName nvarchar(500) NOT NULL PRIMARY KEY,
    CategoryShortDesc nvarchar(1000) NULL,
    CategoryOrder tinyint NOT NULL,
    ParentCategoryName nvarchar(500) NULL
);

INSERT INTO @CategorySeed
(
    CategoryName,
    CategoryShortDesc,
    CategoryOrder,
    ParentCategoryName
)
VALUES
    (N'Phần mềm doanh nghiệp', N'Các nền tảng phục vụ quản trị và vận hành doanh nghiệp.', 1, NULL),
    (N'Hạ tầng công nghệ thông tin', N'Máy chủ, mạng, lưu trữ và hạ tầng cloud.', 2, NULL),
    (N'An toàn thông tin', N'Sản phẩm bảo mật hệ thống, dữ liệu và thiết bị đầu cuối.', 3, NULL),
    (N'Thiết bị văn phòng', N'Thiết bị số hóa và hỗ trợ vận hành văn phòng.', 4, NULL),
    (N'Giải pháp dữ liệu', N'Phân tích, sao lưu và chuyển đổi dữ liệu doanh nghiệp.', 5, NULL),
    (N'Quản lý khách hàng', N'Các sản phẩm CRM và chăm sóc khách hàng.', 6, N'Phần mềm doanh nghiệp'),
    (N'Kế toán và ERP', N'Các sản phẩm kế toán, tài chính và hoạch định nguồn lực.', 7, N'Phần mềm doanh nghiệp'),
    (N'Điện toán đám mây', N'Máy chủ ảo, cloud hosting và dịch vụ lưu trữ.', 8, N'Hạ tầng công nghệ thông tin'),
    (N'Thiết bị mạng', N'Gateway, router, switch và thiết bị kết nối.', 9, N'Hạ tầng công nghệ thông tin'),
    (N'Bảo mật thiết bị đầu cuối', N'Giải pháp bảo vệ máy trạm và thiết bị người dùng.', 10, N'An toàn thông tin');

DECLARE @ServiceTypeSeed table
(
    ServiceTypeName nvarchar(200) NOT NULL PRIMARY KEY,
    ServiceOrder tinyint NOT NULL
);

INSERT INTO @ServiceTypeSeed
(
    ServiceTypeName,
    ServiceOrder
)
VALUES
    (N'Tư vấn', 1),
    (N'Triển khai', 2),
    (N'Tích hợp hệ thống', 3),
    (N'Bảo trì', 4),
    (N'Đào tạo', 5),
    (N'Hỗ trợ kỹ thuật', 6),
    (N'Hosting và Cloud', 7),
    (N'Kiểm thử', 8),
    (N'Chuyển đổi dữ liệu', 9),
    (N'An ninh mạng', 10);

DECLARE @ProductSeed table
(
    ProductCode varchar(20) NOT NULL PRIMARY KEY,
    ProductName nvarchar(500) NOT NULL,
    CategoryName nvarchar(500) NOT NULL,
    ProductShortDesc nvarchar(2000) NULL,
    ProductDetails nvarchar(max) NULL,
    ProductFeatures nvarchar(max) NULL,
    ProductBenefit nvarchar(max) NULL,
    ProductPrice float NOT NULL,
    ProductOrder int NOT NULL,
    ProductTags nvarchar(500) NULL
);

INSERT INTO @ProductSeed
(
    ProductCode,
    ProductName,
    CategoryName,
    ProductShortDesc,
    ProductDetails,
    ProductFeatures,
    ProductBenefit,
    ProductPrice,
    ProductOrder,
    ProductTags
)
VALUES
    ('DTC-CONTRACT', N'DTC Contract Hub', N'Phần mềm doanh nghiệp', N'Nền tảng quản lý vòng đời hợp đồng điện tử.', N'Quản lý mẫu, hợp đồng, đàm phán, phê duyệt và truy cập khách hàng.', N'RBAC, versioning, audit, OTP và quản lý tài liệu.', N'Giảm thời gian xử lý và tăng khả năng kiểm soát hợp đồng.', 120000000, 1, N'hợp đồng, số hóa, quản trị'),
    ('DTC-CRM', N'DTC CRM Pro', N'Quản lý khách hàng', N'Giải pháp quản lý khách hàng và lịch sử tương tác.', N'Tập trung dữ liệu khách hàng, cơ hội và hoạt động chăm sóc.', N'Customer 360, interaction timeline và báo cáo.', N'Tăng hiệu quả bán hàng và chăm sóc khách hàng.', 85000000, 2, N'crm, khách hàng, bán hàng'),
    ('DTC-ERP', N'DTC ERP Business', N'Kế toán và ERP', N'Giải pháp hoạch định nguồn lực cho doanh nghiệp.', N'Kết nối quy trình tài chính, mua hàng, bán hàng và kho.', N'Quy trình liên phòng ban và báo cáo quản trị.', N'Chuẩn hóa dữ liệu và giảm thao tác thủ công.', 250000000, 3, N'erp, kế toán, quản trị'),
    ('CLOUD-STD', N'Cloud Server Standard', N'Điện toán đám mây', N'Máy chủ cloud tiêu chuẩn cho hệ thống doanh nghiệp.', N'Cấu hình linh hoạt, giám sát tài nguyên và sao lưu định kỳ.', N'4 vCPU, 8 GB RAM, 200 GB SSD.', N'Triển khai nhanh và dễ dàng mở rộng.', 36000000, 4, N'cloud, server, hosting'),
    ('NETWORK-GW', N'Enterprise Network Gateway', N'Thiết bị mạng', N'Gateway quản lý kết nối mạng doanh nghiệp.', N'Điều phối truy cập và quản lý nhiều đường truyền.', N'VPN, QoS, failover và giám sát băng thông.', N'Tăng độ ổn định và an toàn cho kết nối.', 45000000, 5, N'network, gateway, vpn'),
    ('ENDPOINT-SEC', N'Endpoint Security Suite', N'Bảo mật thiết bị đầu cuối', N'Giải pháp bảo vệ máy trạm và máy chủ.', N'Phát hiện mã độc, hành vi bất thường và quản lý tập trung.', N'Antivirus, EDR và policy management.', N'Giảm rủi ro tấn công trên thiết bị người dùng.', 28000000, 6, N'endpoint, edr, antivirus'),
    ('FIREWALL-ENT', N'Enterprise Firewall Appliance', N'An toàn thông tin', N'Thiết bị tường lửa cho hệ thống doanh nghiệp.', N'Kiểm soát lưu lượng và phát hiện truy cập nguy hiểm.', N'IPS, web filtering, application control và VPN.', N'Bảo vệ lớp biên và tăng khả năng giám sát.', 95000000, 7, N'firewall, ips, security'),
    ('DOC-SCANNER', N'Máy quét tài liệu tốc độ cao', N'Thiết bị văn phòng', N'Thiết bị số hóa hồ sơ và hợp đồng giấy.', N'Hỗ trợ quét hai mặt và xử lý tài liệu theo lô.', N'ADF, duplex, OCR-ready và kết nối mạng.', N'Rút ngắn thời gian số hóa hồ sơ.', 18500000, 8, N'scanner, tài liệu, số hóa'),
    ('DATA-ANALYTICS', N'Data Analytics Platform', N'Giải pháp dữ liệu', N'Nền tảng tổng hợp và phân tích dữ liệu quản trị.', N'Kết nối nhiều nguồn dữ liệu và xây dựng dashboard.', N'ETL, data model, dashboard và cảnh báo.', N'Hỗ trợ ra quyết định dựa trên dữ liệu.', 175000000, 9, N'analytics, dashboard, dữ liệu'),
    ('BACKUP-APPLIANCE', N'Backup Storage Appliance', N'Giải pháp dữ liệu', N'Thiết bị lưu trữ và sao lưu dữ liệu tập trung.', N'Sao lưu theo lịch và phục hồi dữ liệu doanh nghiệp.', N'Incremental backup, encryption và replication.', N'Giảm nguy cơ mất dữ liệu và thời gian gián đoạn.', 78000000, 10, N'backup, storage, dữ liệu');

DECLARE @ServiceSeed table
(
    ServiceName nvarchar(2000) NOT NULL PRIMARY KEY,
    ServiceTypeName nvarchar(200) NOT NULL,
    ServicePrice float NOT NULL,
    SetupPrice float NOT NULL,
    MaintainPrice float NOT NULL,
    ServiceShortDesc nvarchar(max) NULL,
    ServiceContent nvarchar(max) NULL,
    ServiceOrder int NOT NULL,
    Rewrite varchar(300) NULL
);

INSERT INTO @ServiceSeed
(
    ServiceName,
    ServiceTypeName,
    ServicePrice,
    SetupPrice,
    MaintainPrice,
    ServiceShortDesc,
    ServiceContent,
    ServiceOrder,
    Rewrite
)
VALUES
    (N'Tư vấn quy trình quản lý hợp đồng', N'Tư vấn', 25000000, 0, 0, N'Khảo sát và chuẩn hóa quy trình hợp đồng.', N'Phân tích hiện trạng, đề xuất quy trình và lập kế hoạch triển khai.', 1, 'tu-van-quy-trinh-hop-dong'),
    (N'Triển khai DTC Contract Hub', N'Triển khai', 60000000, 15000000, 12000000, N'Cài đặt và cấu hình hệ thống quản lý hợp đồng.', N'Triển khai môi trường, cấu hình tenant và hỗ trợ nghiệm thu.', 2, 'trien-khai-dtc-contract-hub'),
    (N'Tích hợp ERP và CRM', N'Tích hợp hệ thống', 45000000, 10000000, 8000000, N'Tích hợp dữ liệu hợp đồng với ERP và CRM.', N'Phân tích API, xây dựng kết nối và đồng bộ dữ liệu.', 3, 'tich-hop-erp-crm'),
    (N'Bảo trì phần mềm hàng năm', N'Bảo trì', 30000000, 0, 30000000, N'Bảo trì, cập nhật và xử lý lỗi phần mềm.', N'Bao gồm cập nhật định kỳ, xử lý sự cố và báo cáo bảo trì.', 4, 'bao-tri-phan-mem-hang-nam'),
    (N'Đào tạo quản trị viên hệ thống', N'Đào tạo', 12000000, 0, 0, N'Đào tạo vận hành và quản trị hệ thống.', N'Đào tạo theo nhóm, tài liệu hướng dẫn và phiên hỏi đáp.', 5, 'dao-tao-quan-tri-vien'),
    (N'Hỗ trợ kỹ thuật 24/7', N'Hỗ trợ kỹ thuật', 48000000, 0, 48000000, N'Hỗ trợ sự cố kỹ thuật theo SLA.', N'Tiếp nhận 24/7, phân loại mức độ và xử lý theo cam kết.', 6, 'ho-tro-ky-thuat-24-7'),
    (N'Cloud Hosting Standard', N'Hosting và Cloud', 24000000, 5000000, 6000000, N'Cloud hosting cho ứng dụng doanh nghiệp.', N'Giám sát tài nguyên, sao lưu và hỗ trợ vận hành.', 7, 'cloud-hosting-standard'),
    (N'Kiểm thử bảo mật ứng dụng', N'Kiểm thử', 35000000, 0, 0, N'Đánh giá lỗ hổng bảo mật ứng dụng.', N'Kiểm thử theo phạm vi, báo cáo phát hiện và đề xuất khắc phục.', 8, 'kiem-thu-bao-mat-ung-dung'),
    (N'Chuyển đổi dữ liệu hợp đồng', N'Chuyển đổi dữ liệu', 40000000, 10000000, 5000000, N'Chuẩn hóa và nhập dữ liệu hợp đồng cũ.', N'Làm sạch dữ liệu, ánh xạ trường và kiểm tra sau chuyển đổi.', 9, 'chuyen-doi-du-lieu-hop-dong'),
    (N'Đánh giá an ninh hệ thống', N'An ninh mạng', 55000000, 0, 10000000, N'Đánh giá cấu hình và rủi ro an ninh mạng.', N'Rà soát kiến trúc, kiểm tra cấu hình và lập kế hoạch cải thiện.', 10, 'danh-gia-an-ninh-he-thong');

DECLARE @InsertedCategories table
(
    CategoryId tinyint NOT NULL,
    CategoryName nvarchar(500) NOT NULL
);

DECLARE @InsertedServiceTypes table
(
    ServiceTypeId tinyint NOT NULL,
    ServiceTypeName nvarchar(200) NOT NULL
);

DECLARE @InsertedProducts table
(
    ProductId int NOT NULL,
    ProductCode varchar(20) NOT NULL
);

DECLARE @InsertedServices table
(
    ServiceId int NOT NULL,
    ServiceName nvarchar(2000) NOT NULL
);

BEGIN TRY
    BEGIN TRANSACTION;

    -- 1. Danh mục cha.
    INSERT INTO dbo.tbl_Categories
    (
        CategoryName,
        CategoryShortDesc,
        CategoryOrder,
        CategoryParentId,
        LangId,
        Image
    )
    OUTPUT inserted.CategoryId, inserted.CategoryName
    INTO @InsertedCategories (CategoryId, CategoryName)
    SELECT
        source.CategoryName,
        source.CategoryShortDesc,
        source.CategoryOrder,
        NULL,
        @LangId,
        NULL
    FROM @CategorySeed AS source
    WHERE source.ParentCategoryName IS NULL
      AND NOT EXISTS
      (
          SELECT 1
          FROM dbo.tbl_Categories AS existing WITH (UPDLOCK, HOLDLOCK)
          WHERE existing.CategoryName = source.CategoryName
      );

    -- 2. Danh mục con sau khi đã có ID danh mục cha.
    INSERT INTO dbo.tbl_Categories
    (
        CategoryName,
        CategoryShortDesc,
        CategoryOrder,
        CategoryParentId,
        LangId,
        Image
    )
    OUTPUT inserted.CategoryId, inserted.CategoryName
    INTO @InsertedCategories (CategoryId, CategoryName)
    SELECT
        source.CategoryName,
        source.CategoryShortDesc,
        source.CategoryOrder,
        parent.CategoryId,
        @LangId,
        NULL
    FROM @CategorySeed AS source
    INNER JOIN dbo.tbl_Categories AS parent
        ON parent.CategoryName = source.ParentCategoryName
    WHERE source.ParentCategoryName IS NOT NULL
      AND NOT EXISTS
      (
          SELECT 1
          FROM dbo.tbl_Categories AS existing WITH (UPDLOCK, HOLDLOCK)
          WHERE existing.CategoryName = source.CategoryName
      );

    IF EXISTS
    (
        SELECT 1
        FROM @CategorySeed AS source
        WHERE NOT EXISTS
        (
            SELECT 1
            FROM dbo.tbl_Categories AS category
            WHERE category.CategoryName = source.CategoryName
        )
    )
    BEGIN
        THROW 50022, N'Không tạo hoặc tìm thấy đầy đủ danh mục.', 1;
    END;

    -- 3. Loại dịch vụ.
    INSERT INTO dbo.tbl_ServiceTypes
    (
        ServiceTypeName,
        LangId
    )
    OUTPUT inserted.ServiceTypeId, inserted.ServiceTypeName
    INTO @InsertedServiceTypes (ServiceTypeId, ServiceTypeName)
    SELECT
        source.ServiceTypeName,
        @LangId
    FROM @ServiceTypeSeed AS source
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.tbl_ServiceTypes AS existing WITH (UPDLOCK, HOLDLOCK)
        WHERE existing.ServiceTypeName = source.ServiceTypeName
    );

    IF EXISTS
    (
        SELECT 1
        FROM @ServiceTypeSeed AS source
        WHERE NOT EXISTS
        (
            SELECT 1
            FROM dbo.tbl_ServiceTypes AS serviceType
            WHERE serviceType.ServiceTypeName = source.ServiceTypeName
        )
    )
    BEGIN
        THROW 50023, N'Không tạo hoặc tìm thấy đầy đủ loại dịch vụ.', 1;
    END;

    -- 4. Sản phẩm gắn với danh mục qua tên seed.
    INSERT INTO dbo.tbl_Products
    (
        ProductCode,
        ProductName,
        CategoryId,
        ProductShortDesc,
        ProductDetails,
        ProductFeatures,
        ProductBenefit,
        ProductPrice,
        LangId,
        Status,
        ProductOrder,
        ProductTags,
        ProductCreatedDate,
        Others
    )
    OUTPUT inserted.ProductId, inserted.ProductCode
    INTO @InsertedProducts (ProductId, ProductCode)
    SELECT
        source.ProductCode,
        source.ProductName,
        category.CategoryId,
        source.ProductShortDesc,
        source.ProductDetails,
        source.ProductFeatures,
        source.ProductBenefit,
        source.ProductPrice,
        @LangId,
        1,
        source.ProductOrder,
        source.ProductTags,
        GETDATE(),
        N'Dữ liệu test catalog'
    FROM @ProductSeed AS source
    INNER JOIN dbo.tbl_Categories AS category
        ON category.CategoryName = source.CategoryName
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.tbl_Products AS existing WITH (UPDLOCK, HOLDLOCK)
        WHERE existing.ProductCode = source.ProductCode
    );

    IF EXISTS
    (
        SELECT 1
        FROM @ProductSeed AS source
        WHERE NOT EXISTS
        (
            SELECT 1
            FROM dbo.tbl_Products AS product
            WHERE product.ProductCode = source.ProductCode
        )
    )
    BEGIN
        THROW 50024, N'Không tạo hoặc tìm thấy đầy đủ sản phẩm.', 1;
    END;

    -- 5. Dịch vụ gắn với loại dịch vụ qua tên seed.
    INSERT INTO dbo.tbl_Services
    (
        ServiceName,
        ServiceParentId,
        UserCreated,
        DateCreated,
        Status,
        ServicePrice,
        ServiceTypeId,
        LangId,
        SetupPrice,
        MaintainPrice,
        HasChild,
        ServiceContent,
        ServiceOrder,
        ServiceShortDesc,
        Others,
        Rewrite,
        TitleBrowser,
        MetaKeyword,
        MetaDescription
    )
    OUTPUT inserted.ServiceId, inserted.ServiceName
    INTO @InsertedServices (ServiceId, ServiceName)
    SELECT
        source.ServiceName,
        NULL,
        @CreatedByEmployeeId,
        GETDATE(),
        1,
        source.ServicePrice,
        serviceType.ServiceTypeId,
        @LangId,
        source.SetupPrice,
        source.MaintainPrice,
        0,
        source.ServiceContent,
        source.ServiceOrder,
        source.ServiceShortDesc,
        N'Dữ liệu test catalog',
        source.Rewrite,
        source.ServiceName,
        source.ServiceName,
        source.ServiceShortDesc
    FROM @ServiceSeed AS source
    INNER JOIN dbo.tbl_ServiceTypes AS serviceType
        ON serviceType.ServiceTypeName = source.ServiceTypeName
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.tbl_Services AS existing WITH (UPDLOCK, HOLDLOCK)
        WHERE existing.ServiceName = source.ServiceName
    );

    IF EXISTS
    (
        SELECT 1
        FROM @ServiceSeed AS source
        WHERE NOT EXISTS
        (
            SELECT 1
            FROM dbo.tbl_Services AS service
            WHERE service.ServiceName = source.ServiceName
        )
    )
    BEGIN
        THROW 50025, N'Không tạo hoặc tìm thấy đầy đủ dịch vụ.', 1;
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;

-- Result set 1: danh mục.
SELECT
    category.CategoryId,
    category.CategoryName,
    category.CategoryShortDesc,
    category.CategoryOrder,
    category.CategoryParentId,
    category.LangId,
    CASE WHEN inserted.CategoryId IS NULL THEN 'Already existed' ELSE 'Inserted' END AS ScriptResult
FROM dbo.tbl_Categories AS category
INNER JOIN @CategorySeed AS source
    ON source.CategoryName = category.CategoryName
LEFT JOIN @InsertedCategories AS inserted
    ON inserted.CategoryId = category.CategoryId
ORDER BY category.CategoryOrder, category.CategoryId;

-- Result set 2: loại dịch vụ.
SELECT
    serviceType.ServiceTypeId,
    serviceType.ServiceTypeName,
    serviceType.LangId,
    CASE WHEN inserted.ServiceTypeId IS NULL THEN 'Already existed' ELSE 'Inserted' END AS ScriptResult
FROM dbo.tbl_ServiceTypes AS serviceType
INNER JOIN @ServiceTypeSeed AS source
    ON source.ServiceTypeName = serviceType.ServiceTypeName
LEFT JOIN @InsertedServiceTypes AS inserted
    ON inserted.ServiceTypeId = serviceType.ServiceTypeId
ORDER BY serviceType.ServiceTypeId;

-- Result set 3: sản phẩm.
SELECT
    product.ProductId,
    product.ProductCode,
    product.ProductName,
    product.CategoryId,
    category.CategoryName,
    product.ProductPrice,
    product.Status,
    product.LangId,
    CASE WHEN inserted.ProductId IS NULL THEN 'Already existed' ELSE 'Inserted' END AS ScriptResult
FROM dbo.tbl_Products AS product
INNER JOIN @ProductSeed AS source
    ON source.ProductCode = product.ProductCode
LEFT JOIN dbo.tbl_Categories AS category
    ON category.CategoryId = product.CategoryId
LEFT JOIN @InsertedProducts AS inserted
    ON inserted.ProductId = product.ProductId
ORDER BY product.ProductOrder, product.ProductId;

-- Result set 4: dịch vụ.
SELECT
    service.ServiceId,
    service.ServiceName,
    service.ServiceTypeId,
    serviceType.ServiceTypeName,
    service.ServicePrice,
    service.SetupPrice,
    service.MaintainPrice,
    service.Status,
    service.LangId,
    CASE WHEN inserted.ServiceId IS NULL THEN 'Already existed' ELSE 'Inserted' END AS ScriptResult
FROM dbo.tbl_Services AS service
INNER JOIN @ServiceSeed AS source
    ON source.ServiceName = service.ServiceName
LEFT JOIN dbo.tbl_ServiceTypes AS serviceType
    ON serviceType.ServiceTypeId = service.ServiceTypeId
LEFT JOIN @InsertedServices AS inserted
    ON inserted.ServiceId = service.ServiceId
ORDER BY service.ServiceOrder, service.ServiceId;
