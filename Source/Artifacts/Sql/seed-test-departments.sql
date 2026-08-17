/*
  DEV/TEST ONLY

  Tạo 10 phòng ban active trong tenant hungnd.
  Script có thể chạy lại: DepartmentCode đã tồn tại sẽ được bỏ qua.

  Cột legacy của database là Stutus (không phải Status):
    1 = Active
    0 = Inactive
*/

USE [ContractManagement_Tenant_hungnd];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @LangId int = 1;

IF DB_NAME() <> 'ContractManagement_Tenant_hungnd'
BEGIN
    THROW 50010, N'Sai database đích. Hãy kiểm tra lại câu lệnh USE.', 1;
END;

DECLARE @Departments table
(
    DepartmentCode varchar(20) NOT NULL PRIMARY KEY,
    DepartmentName nvarchar(200) NOT NULL
);

INSERT INTO @Departments
(
    DepartmentCode,
    DepartmentName
)
VALUES
    ('BGD',      N'Ban Giám đốc'),
    ('KD',       N'Phòng Kinh doanh'),
    ('MKT',      N'Phòng Marketing'),
    ('HCNS',     N'Phòng Hành chính - Nhân sự'),
    ('KTTC',     N'Phòng Kế toán - Tài chính'),
    ('KTHUAT',   N'Phòng Kỹ thuật'),
    ('CNTT',     N'Phòng Công nghệ thông tin'),
    ('CSKH',     N'Phòng Chăm sóc khách hàng'),
    ('PHAPCHE',  N'Phòng Pháp chế'),
    ('QLDA',     N'Phòng Quản lý dự án');

DECLARE @Inserted table
(
    DepartmentID smallint NOT NULL,
    DepartmentCode varchar(20) NOT NULL
);

BEGIN TRY
    BEGIN TRANSACTION;

    INSERT INTO dbo.tbl_Department
    (
        DepartmentCode,
        DepartmentName,
        ModifiedDate,
        Stutus,
        LangId
    )
    OUTPUT
        inserted.DepartmentID,
        inserted.DepartmentCode
    INTO @Inserted
    (
        DepartmentID,
        DepartmentCode
    )
    SELECT
        source.DepartmentCode,
        source.DepartmentName,
        GETDATE(),
        1,
        @LangId
    FROM @Departments AS source
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.tbl_Department AS existing WITH (UPDLOCK, HOLDLOCK)
        WHERE existing.DepartmentCode = source.DepartmentCode
    );

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;

SELECT
    department.DepartmentID,
    department.DepartmentCode,
    department.DepartmentName,
    department.ModifiedDate,
    department.Stutus,
    department.LangId,
    CASE
        WHEN inserted.DepartmentID IS NULL THEN 'Already existed'
        ELSE 'Inserted'
    END AS ScriptResult
FROM dbo.tbl_Department AS department
INNER JOIN @Departments AS source
    ON source.DepartmentCode = department.DepartmentCode
LEFT JOIN @Inserted AS inserted
    ON inserted.DepartmentID = department.DepartmentID
ORDER BY department.DepartmentID;
