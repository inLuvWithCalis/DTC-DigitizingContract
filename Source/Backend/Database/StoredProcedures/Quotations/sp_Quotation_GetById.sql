/*
    Created by : Minh Thong
    Create date: 19/06/2026 (dd/mm/yyyy)
    Description: This stored procedure retrieves a quotation by its ID.
    --
    File:$Database\StoredProcedures\Quotations\sp_Quotation_GetById.sql
*/
CREATE OR ALTER PROCEDURE dbo.sp_Quotation_GetById
    @QuotationId INT
AS
BEGIN
    SET NOCOUNT ON;

-- Kết quả thứ nhất: thông tin báo giá
    SELECT
    QuotationId,
    QuotationNo,
    CustomerId,
    QuotationDate,
    TotalAmount,
    QuatationStatus
    FROM dbo.tbl_Quotation
    WHERE QuotationId = @QuotationId;

-- Kết quả thứ hai: danh sách sản phẩm
    SELECT
        detail.ProductId,
    product.ProductName,
    detail.Quantity,
    detail.UnitPrice,
    detail.Amount
    FROM dbo.tbl_QuotationDetail AS detail
    LEFT JOIN dbo.tbl_Products AS product
        ON product.ProductId = detail.ProductId
    WHERE detail.QuotationId = @QuotationId;
END;