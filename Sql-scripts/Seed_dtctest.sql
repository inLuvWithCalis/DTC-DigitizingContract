-- ====================================================================
-- Script: Seed dữ liệu cho Database Tenant dtctest
-- Database: ContractManagement_Tenant_dtctest
-- Yêu cầu tài khoản:
--   1. admin  - mk: 123 - role 3 (AdminOfficer)
--   2. admin2 - mk: 123 - role 6 (Manager)
--   3. admin1 - mk: 123 - role 1 (Sale)
--   4. admin3 - mk: 123 - role 4 (Technical)
-- Kèm theo: Dữ liệu mẫu Phòng ban, Khách hàng, Danh mục, Sản phẩm, Dịch vụ, Hồ sơ pháp lý Tenant
-- ====================================================================

USE [ContractManagement_Tenant_dtctest]
GO

SET NOCOUNT ON;

PRINT N'>>> BẮT ĐẦU SEED DỮ LIỆU CHO DATABASE ContractManagement_Tenant_dtctest...';

-- Hash của mật khẩu '123' sinh bởi Microsoft.AspNetCore.Identity.PasswordHasher (PBKDF2 HMAC-SHA256, 100,000 iterations)
DECLARE @PasswordHash NVARCHAR(255) = N'AQAAAAIAAYagAAAAEG6MOECss/dJofhDzdz9izTOy0Wh03KaM6mA7qf8O3TsnPUAEx8+3D/p9X2pO7gb+Q==';

-- -------------------------------------------------------------------
-- 1. SEED BẢNG tbl_Department (Phòng ban)
-- -------------------------------------------------------------------
PRINT N'1. Đang seed bảng tbl_Department...';

IF NOT EXISTS (SELECT 1 FROM [tbl_Department] WHERE [DepartmentCode] = 'BGD')
    INSERT INTO [tbl_Department] ([DepartmentCode], [DepartmentName], [ModifiedDate], [Stutus], [LangId])
    VALUES ('BGD', N'Ban Giám Đốc', GETUTCDATE(), 1, 1);
ELSE
    UPDATE [tbl_Department] SET [DepartmentName] = N'Ban Giám Đốc', [Stutus] = 1, [LangId] = 1 WHERE [DepartmentCode] = 'BGD';

IF NOT EXISTS (SELECT 1 FROM [tbl_Department] WHERE [DepartmentCode] = 'HCNS')
    INSERT INTO [tbl_Department] ([DepartmentCode], [DepartmentName], [ModifiedDate], [Stutus], [LangId])
    VALUES ('HCNS', N'Phòng Hành Chính Nhân Sự', GETUTCDATE(), 1, 1);
ELSE
    UPDATE [tbl_Department] SET [DepartmentName] = N'Phòng Hành Chính Nhân Sự', [Stutus] = 1, [LangId] = 1 WHERE [DepartmentCode] = 'HCNS';

IF NOT EXISTS (SELECT 1 FROM [tbl_Department] WHERE [DepartmentCode] = 'KD')
    INSERT INTO [tbl_Department] ([DepartmentCode], [DepartmentName], [ModifiedDate], [Stutus], [LangId])
    VALUES ('KD', N'Phòng Kinh Doanh', GETUTCDATE(), 1, 1);
ELSE
    UPDATE [tbl_Department] SET [DepartmentName] = N'Phòng Kinh Doanh', [Stutus] = 1, [LangId] = 1 WHERE [DepartmentCode] = 'KD';

IF NOT EXISTS (SELECT 1 FROM [tbl_Department] WHERE [DepartmentCode] = 'KT')
    INSERT INTO [tbl_Department] ([DepartmentCode], [DepartmentName], [ModifiedDate], [Stutus], [LangId])
    VALUES ('KT', N'Phòng Kỹ Thuật - Triển Khai', GETUTCDATE(), 1, 1);
ELSE
    UPDATE [tbl_Department] SET [DepartmentName] = N'Phòng Kỹ Thuật - Triển Khai', [Stutus] = 1, [LangId] = 1 WHERE [DepartmentCode] = 'KT';

IF NOT EXISTS (SELECT 1 FROM [tbl_Department] WHERE [DepartmentCode] = 'KTTC')
    INSERT INTO [tbl_Department] ([DepartmentCode], [DepartmentName], [ModifiedDate], [Stutus], [LangId])
    VALUES ('KTTC', N'Phòng Kế Toán Tài Chính', GETUTCDATE(), 1, 1);
ELSE
    UPDATE [tbl_Department] SET [DepartmentName] = N'Phòng Kế Toán Tài Chính', [Stutus] = 1, [LangId] = 1 WHERE [DepartmentCode] = 'KTTC';

DECLARE @DeptId_BGD INT = (SELECT TOP 1 DepartmentID FROM [tbl_Department] WHERE [DepartmentCode] = 'BGD');
DECLARE @DeptId_HCNS INT = (SELECT TOP 1 DepartmentID FROM [tbl_Department] WHERE [DepartmentCode] = 'HCNS');
DECLARE @DeptId_KD INT = (SELECT TOP 1 DepartmentID FROM [tbl_Department] WHERE [DepartmentCode] = 'KD');
DECLARE @DeptId_KT INT = (SELECT TOP 1 DepartmentID FROM [tbl_Department] WHERE [DepartmentCode] = 'KT');

-- -------------------------------------------------------------------
-- 2. SEED BẢNG tbl_Employee (Nhân viên / Tài khoản)
-- -------------------------------------------------------------------
PRINT N'2. Đang seed bảng tbl_Employee...';

-- Account 1: admin - mk: 123 - role 3 (AdminOfficer)
IF NOT EXISTS (SELECT 1 FROM [tbl_Employee] WHERE [EmployeeAccount] = 'admin')
BEGIN
    INSERT INTO [tbl_Employee] (
        [EmployeeCode], [EmployeeAccount], [EmployeePassword], [EmployeeFullName],
        [EmployeeMobile], [EmployeeEmail], [EmployeeType], [DepartmentId],
        [Status], [MustChangePassword], [SessionVersion], [DateCreated]
    )
    VALUES (
        'EMP001', 'admin', @PasswordHash, N'Hành Chính Tổng Hợp (admin)',
        '0901234001', 'admin@dtctech.vn', 3, @DeptId_HCNS,
        1, 0, 1, GETUTCDATE()
    );
END
ELSE
BEGIN
    UPDATE [tbl_Employee]
    SET [EmployeeFullName] = N'Hành Chính Tổng Hợp (admin)',
        [EmployeePassword] = @PasswordHash,
        [EmployeeType] = 3,
        [DepartmentId] = @DeptId_HCNS,
        [Status] = 1,
        [MustChangePassword] = 0,
        [SessionVersion] = 1
    WHERE [EmployeeAccount] = 'admin';
END

-- Account 2: admin2 - mk: 123 - role 6 (Manager)
IF NOT EXISTS (SELECT 1 FROM [tbl_Employee] WHERE [EmployeeAccount] = 'admin2')
BEGIN
    INSERT INTO [tbl_Employee] (
        [EmployeeCode], [EmployeeAccount], [EmployeePassword], [EmployeeFullName],
        [EmployeeMobile], [EmployeeEmail], [EmployeeType], [DepartmentId],
        [Status], [MustChangePassword], [SessionVersion], [DateCreated]
    )
    VALUES (
        'EMP002', 'admin2', @PasswordHash, N'Giám Đốc Điều Hành (admin2)',
        '0901234002', 'admin2@dtctech.vn', 6, @DeptId_BGD,
        1, 0, 1, GETUTCDATE()
    );
END
ELSE
BEGIN
    UPDATE [tbl_Employee]
    SET [EmployeeFullName] = N'Giám Đốc Điều Hành (admin2)',
        [EmployeePassword] = @PasswordHash,
        [EmployeeType] = 6,
        [DepartmentId] = @DeptId_BGD,
        [Status] = 1,
        [MustChangePassword] = 0,
        [SessionVersion] = 1
    WHERE [EmployeeAccount] = 'admin2';
END

-- Account 3: admin1 - mk: 123 - role 1 (Sale)
IF NOT EXISTS (SELECT 1 FROM [tbl_Employee] WHERE [EmployeeAccount] = 'admin1')
BEGIN
    INSERT INTO [tbl_Employee] (
        [EmployeeCode], [EmployeeAccount], [EmployeePassword], [EmployeeFullName],
        [EmployeeMobile], [EmployeeEmail], [EmployeeType], [DepartmentId],
        [Status], [MustChangePassword], [SessionVersion], [DateCreated]
    )
    VALUES (
        'EMP003', 'admin1', @PasswordHash, N'Chuyên Viên Kinh Doanh (admin1)',
        '0901234003', 'admin1@dtctech.vn', 1, @DeptId_KD,
        1, 0, 1, GETUTCDATE()
    );
END
ELSE
BEGIN
    UPDATE [tbl_Employee]
    SET [EmployeeFullName] = N'Chuyên Viên Kinh Doanh (admin1)',
        [EmployeePassword] = @PasswordHash,
        [EmployeeType] = 1,
        [DepartmentId] = @DeptId_KD,
        [Status] = 1,
        [MustChangePassword] = 0,
        [SessionVersion] = 1
    WHERE [EmployeeAccount] = 'admin1';
END

-- Account 4: admin3 - mk: 123 - role 4 (Technical)
IF NOT EXISTS (SELECT 1 FROM [tbl_Employee] WHERE [EmployeeAccount] = 'admin3')
BEGIN
    INSERT INTO [tbl_Employee] (
        [EmployeeCode], [EmployeeAccount], [EmployeePassword], [EmployeeFullName],
        [EmployeeMobile], [EmployeeEmail], [EmployeeType], [DepartmentId],
        [Status], [MustChangePassword], [SessionVersion], [DateCreated]
    )
    VALUES (
        'EMP004', 'admin3', @PasswordHash, N'Kỹ Sư Triển Khai (admin3)',
        '0901234004', 'admin3@dtctech.vn', 4, @DeptId_KT,
        1, 0, 1, GETUTCDATE()
    );
END
ELSE
BEGIN
    UPDATE [tbl_Employee]
    SET [EmployeeFullName] = N'Kỹ Sư Triển Khai (admin3)',
        [EmployeePassword] = @PasswordHash,
        [EmployeeType] = 4,
        [DepartmentId] = @DeptId_KT,
        [Status] = 1,
        [MustChangePassword] = 0,
        [SessionVersion] = 1
    WHERE [EmployeeAccount] = 'admin3';
END

DECLARE @EmpId_Manager INT = (SELECT TOP 1 EmployeeId FROM [tbl_Employee] WHERE [EmployeeAccount] = 'admin2');
DECLARE @EmpId_Sale INT = (SELECT TOP 1 EmployeeId FROM [tbl_Employee] WHERE [EmployeeAccount] = 'admin1');

-- -------------------------------------------------------------------
-- 3. SEED BẢNG tbl_TenantLegalProfile (Hồ sơ pháp lý của Tenant dtctest)
-- -------------------------------------------------------------------
PRINT N'3. Đang seed bảng tbl_TenantLegalProfile...';

IF NOT EXISTS (SELECT 1 FROM [tbl_TenantLegalProfile])
BEGIN
    INSERT INTO [tbl_TenantLegalProfile] (
        [TenantLegalProfileId], [LegalEntityName], [TaxCode], [Address], [RepresentativeName], [RepresentativeTitle],
        [BankAccountNumber], [BankName], [PhoneNumber], [FaxNumber],
        [CreatedByEmployeeId], [CreatedAt], [UpdatedByEmployeeId], [UpdatedAt]
    )
    VALUES (
        1,
        N'CÔNG TY CỔ PHẦN CÔNG NGHỆ DTC VIỆT NAM',
        '0108998877',
        N'Tầng 8, Tòa nhà DTC Tower, Số 18 Phố Duy Tân, Cầu Giấy, Hà Nội',
        N'Giám Đốc Điều Hành',
        N'Tổng Giám Đốc',
        '19035678901234',
        N'Techcombank - Chi nhánh Thăng Long',
        '02437899999',
        '02437899998',
        @EmpId_Manager,
        GETUTCDATE(),
        @EmpId_Manager,
        GETUTCDATE()
    );
END
ELSE
BEGIN
    UPDATE [tbl_TenantLegalProfile]
    SET [LegalEntityName] = N'CÔNG TY CỔ PHẦN CÔNG NGHỆ DTC VIỆT NAM',
        [TaxCode] = '0108998877',
        [Address] = N'Tầng 8, Tòa nhà DTC Tower, Số 18 Phố Duy Tân, Cầu Giấy, Hà Nội',
        [RepresentativeName] = N'Giám Đốc Điều Hành',
        [RepresentativeTitle] = N'Tổng Giám Đốc',
        [BankAccountNumber] = '19035678901234',
        [BankName] = N'Techcombank - Chi nhánh Thăng Long',
        [PhoneNumber] = '02437899999',
        [FaxNumber] = '02437899998',
        [UpdatedByEmployeeId] = @EmpId_Manager,
        [UpdatedAt] = GETUTCDATE()
    WHERE [TenantLegalProfileId] = 1;
END

-- -------------------------------------------------------------------
-- 4. SEED BẢNG tbl_Categories (Danh mục sản phẩm/dịch vụ)
-- -------------------------------------------------------------------
PRINT N'4. Đang seed bảng tbl_Categories...';

IF NOT EXISTS (SELECT 1 FROM [tbl_Categories] WHERE [CategoryName] LIKE N'%Phần mềm%')
BEGIN
    INSERT INTO [tbl_Categories] ([CategoryName], [CategoryShortDesc], [CategoryOrder], [LangId])
    VALUES (N'Giải pháp Phần mềm Doanh nghiệp', N'Các phần mềm ERP, CRM, Quản lý hợp đồng số', 1, 1);
END
ELSE
BEGIN
    UPDATE [tbl_Categories] 
    SET [CategoryName] = N'Giải pháp Phần mềm Doanh nghiệp',
        [CategoryShortDesc] = N'Các phần mềm ERP, CRM, Quản lý hợp đồng số'
    WHERE [CategoryName] LIKE N'%Phần mềm%';
END

IF NOT EXISTS (SELECT 1 FROM [tbl_Categories] WHERE [CategoryName] LIKE N'%Dịch vụ Công nghệ%')
BEGIN
    INSERT INTO [tbl_Categories] ([CategoryName], [CategoryShortDesc], [CategoryOrder], [LangId])
    VALUES (N'Dịch vụ Công nghệ Thông tin', N'Dịch vụ triển khai, bảo trì hạ tầng, tư vấn giải pháp', 2, 1);
END
ELSE
BEGIN
    UPDATE [tbl_Categories]
    SET [CategoryName] = N'Dịch vụ Công nghệ Thông tin',
        [CategoryShortDesc] = N'Dịch vụ triển khai, bảo trì hạ tầng, tư vấn giải pháp'
    WHERE [CategoryName] LIKE N'%Dịch vụ Công nghệ%';
END

IF NOT EXISTS (SELECT 1 FROM [tbl_Categories] WHERE [CategoryName] LIKE N'%Thiết bị%')
BEGIN
    INSERT INTO [tbl_Categories] ([CategoryName], [CategoryShortDesc], [CategoryOrder], [LangId])
    VALUES (N'Thiết bị & Hạ tầng Máy chủ', N'Máy chủ chuyên dụng, thiết bị lưu trữ SAN/NAS, thiết bị mạng', 3, 1);
END
ELSE
BEGIN
    UPDATE [tbl_Categories]
    SET [CategoryName] = N'Thiết bị & Hạ tầng Máy chủ',
        [CategoryShortDesc] = N'Máy chủ chuyên dụng, thiết bị lưu trữ SAN/NAS, thiết bị mạng'
    WHERE [CategoryName] LIKE N'%Thiết bị%';
END

DECLARE @CatId_Software INT = (SELECT TOP 1 CategoryId FROM [tbl_Categories] WHERE [CategoryName] LIKE N'%Phần mềm%');
DECLARE @CatId_Hardware INT = (SELECT TOP 1 CategoryId FROM [tbl_Categories] WHERE [CategoryName] LIKE N'%Thiết bị%');

-- -------------------------------------------------------------------
-- 5. SEED BẢNG tbl_Products (Sản phẩm)
-- -------------------------------------------------------------------
PRINT N'5. Đang seed bảng tbl_Products...';

IF NOT EXISTS (SELECT 1 FROM [tbl_Products] WHERE [ProductCode] = 'SFT-DTC-01')
BEGIN
    INSERT INTO [tbl_Products] (
        [ProductCode], [ProductName], [ProductShortName], [CategoryId],
        [ProductShortDesc], [ProductPrice], [Status], [LangId], [ProductCreatedDate]
    )
    VALUES (
        'SFT-DTC-01',
        N'Phần mềm Quản lý & Số hóa Hợp đồng DTC Contract v2.0',
        N'DTC Contract v2.0',
        @CatId_Software,
        N'Hệ thống quản lý và số hóa quy trình ký duyệt hợp đồng điện tử trọn vòng đời cho doanh nghiệp.',
        35000000,
        1, 1, GETUTCDATE()
    );
END
ELSE
BEGIN
    UPDATE [tbl_Products]
    SET [ProductName] = N'Phần mềm Quản lý & Số hóa Hợp đồng DTC Contract v2.0',
        [ProductShortName] = N'DTC Contract v2.0',
        [ProductShortDesc] = N'Hệ thống quản lý và số hóa quy trình ký duyệt hợp đồng điện tử trọn vòng đời cho doanh nghiệp.',
        [ProductPrice] = 35000000,
        [Status] = 1
    WHERE [ProductCode] = 'SFT-DTC-01';
END

IF NOT EXISTS (SELECT 1 FROM [tbl_Products] WHERE [ProductCode] = 'SFT-CRM-02')
BEGIN
    INSERT INTO [tbl_Products] (
        [ProductCode], [ProductName], [ProductShortName], [CategoryId],
        [ProductShortDesc], [ProductPrice], [Status], [LangId], [ProductCreatedDate]
    )
    VALUES (
        'SFT-CRM-02',
        N'Phần mềm Quản lý Quan hệ Khách hàng DTC CRM Pro',
        N'DTC CRM Pro',
        @CatId_Software,
        N'Giải pháp tối ưu hóa dữ liệu khách hàng, phễu bán hàng và chăm sóc tự động đa kênh.',
        25000000,
        1, 1, GETUTCDATE()
    );
END
ELSE
BEGIN
    UPDATE [tbl_Products]
    SET [ProductName] = N'Phần mềm Quản lý Quan hệ Khách hàng DTC CRM Pro',
        [ProductShortName] = N'DTC CRM Pro',
        [ProductShortDesc] = N'Giải pháp tối ưu hóa dữ liệu khách hàng, phễu bán hàng và chăm sóc tự động đa kênh.',
        [ProductPrice] = 25000000,
        [Status] = 1
    WHERE [ProductCode] = 'SFT-CRM-02';
END

IF NOT EXISTS (SELECT 1 FROM [tbl_Products] WHERE [ProductCode] = 'HW-SRV-DELL')
BEGIN
    INSERT INTO [tbl_Products] (
        [ProductCode], [ProductName], [ProductShortName], [CategoryId],
        [ProductShortDesc], [ProductPrice], [Status], [LangId], [ProductCreatedDate]
    )
    VALUES (
        'HW-SRV-DELL',
        N'Máy chủ Dell PowerEdge R750xs 2U Rack Server',
        N'Dell PowerEdge R750xs',
        @CatId_Hardware,
        N'Máy chủ hiệu năng cao 2 socket Intel Xeon Silver, 64GB RAM ECC, 2x 960GB SSD Enterprise.',
        85000000,
        1, 1, GETUTCDATE()
    );
END
ELSE
BEGIN
    UPDATE [tbl_Products]
    SET [ProductName] = N'Máy chủ Dell PowerEdge R750xs 2U Rack Server',
        [ProductShortName] = N'Dell PowerEdge R750xs',
        [ProductShortDesc] = N'Máy chủ hiệu năng cao 2 socket Intel Xeon Silver, 64GB RAM ECC, 2x 960GB SSD Enterprise.',
        [ProductPrice] = 85000000,
        [Status] = 1
    WHERE [ProductCode] = 'HW-SRV-DELL';
END

-- -------------------------------------------------------------------
-- 6. SEED BẢNG tbl_Services (Dịch vụ)
-- -------------------------------------------------------------------
PRINT N'6. Đang seed bảng tbl_Services...';

IF NOT EXISTS (SELECT 1 FROM [tbl_Services] WHERE [ServiceName] LIKE N'%Triển khai & Chuyển giao%')
BEGIN
    INSERT INTO [tbl_Services] (
        [ServiceName], [ServiceShortDesc], [ServicePrice], [Status], [LangId], [DateCreated]
    )
    VALUES (
        N'Dịch vụ Triển khai & Chuyển giao Hệ thống DTC Contract',
        N'Cài đặt cấu hình môi trường on-premise/cloud, đào tạo nhân viên sử dụng và chuẩn hóa biểu mẫu.',
        15000000,
        1, 1, GETUTCDATE()
    );
END
ELSE
BEGIN
    UPDATE [tbl_Services]
    SET [ServiceName] = N'Dịch vụ Triển khai & Chuyển giao Hệ thống DTC Contract',
        [ServiceShortDesc] = N'Cài đặt cấu hình môi trường on-premise/cloud, đào tạo nhân viên sử dụng và chuẩn hóa biểu mẫu.',
        [ServicePrice] = 15000000,
        [Status] = 1
    WHERE [ServiceName] LIKE N'%Triển khai & Chuyển giao%';
END

IF NOT EXISTS (SELECT 1 FROM [tbl_Services] WHERE [ServiceName] LIKE N'%Bảo trì & Hỗ trợ Kỹ thuật%')
BEGIN
    INSERT INTO [tbl_Services] (
        [ServiceName], [ServiceShortDesc], [ServicePrice], [Status], [LangId], [DateCreated]
    )
    VALUES (
        N'Gói Bảo trì & Hỗ trợ Kỹ thuật Tiêu chuẩn (12 Tháng)',
        N'Hỗ trợ kỹ thuật 24/7 SLA 4h, sao lưu dữ liệu tự động hàng tuần, cập nhật các bản vá bảo mật định kỳ.',
        12000000,
        1, 1, GETUTCDATE()
    );
END
ELSE
BEGIN
    UPDATE [tbl_Services]
    SET [ServiceName] = N'Gói Bảo trì & Hỗ trợ Kỹ thuật Tiêu chuẩn (12 Tháng)',
        [ServiceShortDesc] = N'Hỗ trợ kỹ thuật 24/7 SLA 4h, sao lưu dữ liệu tự động hàng tuần, cập nhật các bản vá bảo mật định kỳ.',
        [ServicePrice] = 12000000,
        [Status] = 1
    WHERE [ServiceName] LIKE N'%Bảo trì & Hỗ trợ Kỹ thuật%';
END

IF NOT EXISTS (SELECT 1 FROM [tbl_Services] WHERE [ServiceName] LIKE N'%Cổng Chữ ký số%')
BEGIN
    INSERT INTO [tbl_Services] (
        [ServiceName], [ServiceShortDesc], [ServicePrice], [Status], [LangId], [DateCreated]
    )
    VALUES (
        N'Dịch vụ Tích hợp Cổng Chữ ký số (USB Token / Cloud HSM)',
        N'Kết nối giải pháp chữ ký số HSM/CloudCA cho quy trình ký điện tử pháp lý trực tiếp trên hợp đồng.',
        18000000,
        1, 1, GETUTCDATE()
    );
END
ELSE
BEGIN
    UPDATE [tbl_Services]
    SET [ServiceName] = N'Dịch vụ Tích hợp Cổng Chữ ký số (USB Token / Cloud HSM)',
        [ServiceShortDesc] = N'Kết nối giải pháp chữ ký số HSM/CloudCA cho quy trình ký điện tử pháp lý trực tiếp trên hợp đồng.',
        [ServicePrice] = 18000000,
        [Status] = 1
    WHERE [ServiceName] LIKE N'%Cổng Chữ ký số%';
END

-- -------------------------------------------------------------------
-- 7. SEED BẢNG tbl_Customers (Khách hàng mẫu)
-- -------------------------------------------------------------------
PRINT N'7. Đang seed bảng tbl_Customers...';

IF NOT EXISTS (SELECT 1 FROM [tbl_Customers] WHERE [CustomerCode] = 'KH001')
BEGIN
    INSERT INTO [tbl_Customers] (
        [CustomerCode], [CustomerAccount], [CustomerFullName], [CustomerCompany],
        [CustomerTaxCode], [CustomerAddress], [CustomerEmail], [CustomerPhone], [CustomerMobile],
        [CustomerRepresentativeName], [CustomerRepresentativeTitle],
        [CustomerBankAccountNumber], [CustomerBankName],
        [CustomerContactPersonName], [CustomerContactPersonPhone], [CustomerContactPersonTitle],
        [Status], [DateCreated], [UserCreated]
    )
    VALUES (
        'KH001', 'fpt_telecom', N'Nguyễn Hoàng Long', N'Công ty Cổ phần Viễn thông FPT Telecom',
        '0101778163', N'Tòa nhà FPT, Phố Duy Tân, Phường Dịch Vọng Hậu, Quận Cầu Giấy, Hà Nội',
        'contact@fpttelecom.com.vn', '02473002222', '0912345678',
        N'Hoàng Nam Tiến', N'Chủ tịch HĐQT',
        '0011001234567', N'Vietcombank - Sở giao dịch Hà Nội',
        N'Trần Thị Mai', '0988776655', N'Trưởng phòng CNTT',
        1, GETUTCDATE(), @EmpId_Sale
    );
END
ELSE
BEGIN
    UPDATE [tbl_Customers]
    SET [CustomerFullName] = N'Nguyễn Hoàng Long',
        [CustomerCompany] = N'Công ty Cổ phần Viễn thông FPT Telecom',
        [CustomerAddress] = N'Tòa nhà FPT, Phố Duy Tân, Phường Dịch Vọng Hậu, Quận Cầu Giấy, Hà Nội',
        [CustomerRepresentativeName] = N'Hoàng Nam Tiến',
        [CustomerRepresentativeTitle] = N'Chủ tịch HĐQT',
        [CustomerBankName] = N'Vietcombank - Sở giao dịch Hà Nội',
        [CustomerContactPersonName] = N'Trần Thị Mai',
        [CustomerContactPersonTitle] = N'Trưởng phòng CNTT',
        [Status] = 1
    WHERE [CustomerCode] = 'KH001';
END

IF NOT EXISTS (SELECT 1 FROM [tbl_Customers] WHERE [CustomerCode] = 'KH002')
BEGIN
    INSERT INTO [tbl_Customers] (
        [CustomerCode], [CustomerAccount], [CustomerFullName], [CustomerCompany],
        [CustomerTaxCode], [CustomerAddress], [CustomerEmail], [CustomerPhone], [CustomerMobile],
        [CustomerRepresentativeName], [CustomerRepresentativeTitle],
        [CustomerBankAccountNumber], [CustomerBankName],
        [CustomerContactPersonName], [CustomerContactPersonPhone], [CustomerContactPersonTitle],
        [Status], [DateCreated], [UserCreated]
    )
    VALUES (
        'KH002', 'viettel_corp', N'Trần Đình Trọng', N'Tập đoàn Công nghiệp - Viễn thông Quân đội Viettel',
        '0100109106', N'Lô D26 Khu đô thị mới Cầu Giấy, Phường Yên Hòa, Quận Cầu Giấy, Hà Nội',
        'support@viettel.com.vn', '02462556789', '0977112233',
        N'Tào Đức Thắng', N'Chủ tịch kiêm Tổng Giám Đốc',
        '112000012345', N'MBBank - Chi nhánh Điện Biên Phủ',
        N'Lê Minh Tuấn', '0911223344', N'Giám đốc Dự án',
        1, GETUTCDATE(), @EmpId_Sale
    );
END
ELSE
BEGIN
    UPDATE [tbl_Customers]
    SET [CustomerFullName] = N'Trần Đình Trọng',
        [CustomerCompany] = N'Tập đoàn Công nghiệp - Viễn thông Quân đội Viettel',
        [CustomerAddress] = N'Lô D26 Khu đô thị mới Cầu Giấy, Phường Yên Hòa, Quận Cầu Giấy, Hà Nội',
        [CustomerRepresentativeName] = N'Tào Đức Thắng',
        [CustomerRepresentativeTitle] = N'Chủ tịch kiêm Tổng Giám Đốc',
        [CustomerBankName] = N'MBBank - Chi nhánh Điện Biên Phủ',
        [CustomerContactPersonName] = N'Lê Minh Tuấn',
        [CustomerContactPersonTitle] = N'Giám đốc Dự án',
        [Status] = 1
    WHERE [CustomerCode] = 'KH002';
END

IF NOT EXISTS (SELECT 1 FROM [tbl_Customers] WHERE [CustomerCode] = 'KH003')
BEGIN
    INSERT INTO [tbl_Customers] (
        [CustomerCode], [CustomerAccount], [CustomerFullName], [CustomerCompany],
        [CustomerTaxCode], [CustomerAddress], [CustomerEmail], [CustomerPhone], [CustomerMobile],
        [CustomerRepresentativeName], [CustomerRepresentativeTitle],
        [CustomerBankAccountNumber], [CustomerBankName],
        [CustomerContactPersonName], [CustomerContactPersonPhone], [CustomerContactPersonTitle],
        [Status], [DateCreated], [UserCreated]
    )
    VALUES (
        'KH003', 'saomai_med', N'Phạm Thu Hà', N'Công ty TNHH Thiết bị Y tế Sao Mai',
        '0309988776', N'Số 120 Đường Nguyễn Thị Minh Khai, Phường 6, Quận 3, TP. Hồ Chí Minh',
        'saomaipharm@gmail.com', '02839301234', '0903998877',
        N'Phạm Thu Hà', N'Giám Đốc',
        '060123456789', N'Sacombank - Chi nhánh Tân Định',
        N'Vũ Ngọc Anh', '0933445566', N'Kế toán trưởng',
        1, GETUTCDATE(), @EmpId_Sale
    );
END
ELSE
BEGIN
    UPDATE [tbl_Customers]
    SET [CustomerFullName] = N'Phạm Thu Hà',
        [CustomerCompany] = N'Công ty TNHH Thiết bị Y tế Sao Mai',
        [CustomerAddress] = N'Số 120 Đường Nguyễn Thị Minh Khai, Phường 6, Quận 3, TP. Hồ Chí Minh',
        [CustomerRepresentativeName] = N'Phạm Thu Hà',
        [CustomerRepresentativeTitle] = N'Giám Đốc',
        [CustomerBankName] = N'Sacombank - Chi nhánh Tân Định',
        [CustomerContactPersonName] = N'Vũ Ngọc Anh',
        [CustomerContactPersonTitle] = N'Kế toán trưởng',
        [Status] = 1
    WHERE [CustomerCode] = 'KH003';
END

PRINT N'>>> HOÀN THÀNH SEED DỮ LIỆU CHO ContractManagement_Tenant_dtctest THÀNH CÔNG!';
GO
