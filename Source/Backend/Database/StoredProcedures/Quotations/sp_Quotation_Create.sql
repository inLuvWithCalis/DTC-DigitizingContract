/*
    Created by : Minh Thong
    Create date: 19/06/2026 (dd/mm/yyyy)
    Description: This stored procedure creates a quotation and its quotation details.
    --
    File:$Database\StoredProcedures\Quotations\sp_Quotation_Create.sql
*/
CREATE OR ALTER PROCEDURE dbo.sp_Quotation_Create
    @CustomerId INT,
    @CreatedEmployeeId INT,
    @Items dbo.QuotationItemType READONLY
AS
BEGIN
    SET NOCOUNT ON;

    -- 1. Tính tổng tiền
    DECLARE @TotalAmount DECIMAL(18, 2);

    SELECT @TotalAmount = SUM(Quantity * UnitPrice)
    FROM @Items;

    -- 2. Tạo báo giá
    INSERT INTO dbo.tbl_Quotation
    (
        CustomerId,
        QuotationNo,
        QuotationDate,
        TotalAmount,
        QuatationStatus,
        CreatedEmployeeId
    )
    VALUES
    (
        @CustomerId,
        '',
        GETDATE(),
        @TotalAmount,
        'Draft',
        @CreatedEmployeeId
    );

    -- 3. Lấy ID báo giá vừa tạo
    DECLARE @QuotationId INT;

    SET @QuotationId = SCOPE_IDENTITY();

    -- 4. Tạo mã báo giá
    DECLARE @QuotationNo NVARCHAR(50);

    SET @QuotationNo =
        'QT-' + CAST(@QuotationId AS NVARCHAR(20));

    UPDATE dbo.tbl_Quotation
    SET QuotationNo = @QuotationNo
    WHERE QuotationId = @QuotationId;

    -- 5. Thêm danh sách chi tiết
    INSERT INTO dbo.tbl_QuotationDetail
    (
        QuotationId,
        ProductId,
        Quantity,
        UnitPrice,
        Amount
    )
    SELECT
        @QuotationId,
        ProductId,
        Quantity,
        UnitPrice,
        Quantity * UnitPrice
    FROM @Items;

    -- 6. Trả ID về C#
    SELECT @QuotationId;
END;
GO