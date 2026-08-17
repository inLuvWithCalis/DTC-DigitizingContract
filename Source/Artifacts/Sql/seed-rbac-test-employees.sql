/*
  DEV/TEST ONLY

  Tạo một nhân viên active cho mỗi EmployeeType trong tenant hungnd.
  Mật khẩu được sao chép nguyên giá trị hash từ tài khoản nguồn @SourceAdminAccount.

  EmployeeType:
    1 = Sale
    2 = Marketing
    3 = AdminOfficer
    4 = Technical
    5 = Accountant
    6 = Manager

  Script có thể chạy lại: tài khoản đã tồn tại sẽ không được insert lần nữa.
  RowVersion không được insert vì đây là cột rowversion do SQL Server tự sinh.
*/

USE [ContractManagement_Tenant_hungnd];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @SourceAdminAccount varchar(50) = 'admin';
DECLARE @SourceAdminId int;
DECLARE @SourcePassword varchar(255);
DECLARE @SourceDepartmentId int;

IF DB_NAME() <> 'ContractManagement_Tenant_hungnd'
BEGIN
    THROW 50000, N'Sai database đích. Hãy kiểm tra lại câu lệnh USE.', 1;
END;

IF (
    SELECT COUNT_BIG(*)
    FROM dbo.tbl_Employee
    WHERE EmployeeAccount = @SourceAdminAccount
) <> 1
BEGIN
    THROW 50001, N'Phải có đúng một tài khoản nguồn admin. Hãy sửa @SourceAdminAccount.', 1;
END;

SELECT
    @SourceAdminId = EmployeeId,
    @SourcePassword = EmployeePassword,
    @SourceDepartmentId = DepartmentId
FROM dbo.tbl_Employee
WHERE EmployeeAccount = @SourceAdminAccount;

IF NULLIF(@SourcePassword, '') IS NULL
BEGIN
    THROW 50002, N'Tài khoản nguồn không có mật khẩu để sao chép.', 1;
END;

DECLARE @Roles table
(
    EmployeeType tinyint NOT NULL PRIMARY KEY,
    EmployeeCode varchar(30) NOT NULL,
    EmployeeAccount varchar(50) NOT NULL UNIQUE,
    EmployeeFullName nvarchar(100) NOT NULL
);

INSERT INTO @Roles
(
    EmployeeType,
    EmployeeCode,
    EmployeeAccount,
    EmployeeFullName
)
VALUES
    (1, 'RBAC-SALE',          'test.sale',          N'Test Sale'),
    (2, 'RBAC-MARKETING',     'test.marketing',     N'Test Marketing'),
    (3, 'RBAC-ADMIN-OFFICER', 'test.adminofficer',  N'Test Admin Officer'),
    (4, 'RBAC-TECHNICAL',     'test.technical',     N'Test Technical'),
    (5, 'RBAC-ACCOUNTANT',    'test.accountant',    N'Test Accountant'),
    (6, 'RBAC-MANAGER',       'test.manager',       N'Test Manager');

-- Không âm thầm dùng lại một account đã tồn tại nhưng mang role khác.
IF EXISTS
(
    SELECT 1
    FROM @Roles AS role
    INNER JOIN dbo.tbl_Employee AS employee
        ON employee.EmployeeAccount = role.EmployeeAccount
    WHERE ISNULL(employee.EmployeeType, 0) <> role.EmployeeType
)
BEGIN
    THROW 50003, N'Có tài khoản test đã tồn tại nhưng EmployeeType không khớp.', 1;
END;

DECLARE @Inserted table
(
    EmployeeId int NOT NULL,
    EmployeeAccount varchar(50) NOT NULL,
    EmployeeType tinyint NOT NULL
);

BEGIN TRY
    BEGIN TRANSACTION;

    INSERT INTO dbo.tbl_Employee
    (
        EmployeeCode,
        EmployeeAccount,
        EmployeePassword,
        EmployeeFullName,
        UserCreated,
        DateCreated,
        HireDate,
        Status,
        DepartmentId,
        EmployeeType,
        Others
    )
    OUTPUT
        inserted.EmployeeId,
        inserted.EmployeeAccount,
        inserted.EmployeeType
    INTO @Inserted
    (
        EmployeeId,
        EmployeeAccount,
        EmployeeType
    )
    SELECT
        role.EmployeeCode,
        role.EmployeeAccount,
        @SourcePassword,
        role.EmployeeFullName,
        @SourceAdminId,
        GETUTCDATE(),
        GETUTCDATE(),
        1,
        @SourceDepartmentId,
        role.EmployeeType,
        N'Tài khoản test RBAC; mật khẩu sao chép từ ' + @SourceAdminAccount
    FROM @Roles AS role
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.tbl_Employee AS employee WITH (UPDLOCK, HOLDLOCK)
        WHERE employee.EmployeeAccount = role.EmployeeAccount
    );

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;

SELECT
    employee.EmployeeId,
    employee.EmployeeCode,
    employee.EmployeeAccount,
    employee.EmployeeFullName,
    employee.EmployeeType,
    CASE employee.EmployeeType
        WHEN 1 THEN 'Sale'
        WHEN 2 THEN 'Marketing'
        WHEN 3 THEN 'AdminOfficer'
        WHEN 4 THEN 'Technical'
        WHEN 5 THEN 'Accountant'
        WHEN 6 THEN 'Manager'
        ELSE 'Unknown'
    END AS EmployeeTypeName,
    employee.Status,
    employee.DepartmentId,
    employee.RowVersion,
    CASE WHEN inserted.EmployeeId IS NULL THEN 'Already existed' ELSE 'Inserted' END AS ScriptResult
FROM dbo.tbl_Employee AS employee
INNER JOIN @Roles AS role
    ON role.EmployeeAccount = employee.EmployeeAccount
LEFT JOIN @Inserted AS inserted
    ON inserted.EmployeeId = employee.EmployeeId
ORDER BY employee.EmployeeType;
