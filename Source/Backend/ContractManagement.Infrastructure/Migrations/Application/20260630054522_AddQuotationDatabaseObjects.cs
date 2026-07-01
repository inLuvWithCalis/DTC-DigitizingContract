using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractManagement.Infrastructure.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddQuotationDatabaseObjects : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            /*
             * Tạo Table-Valued Parameter:
             * dbo.QuotationItemType
             *
             * Lưu ý:
             * - Không dùng GO trong migrationBuilder.Sql().
             * - CREATE TYPE phải bọc trong EXEC khi dùng IF.
             */
            migrationBuilder.Sql(
                """
        IF TYPE_ID(N'dbo.QuotationItemType') IS NULL
        BEGIN
            EXEC(N'
                CREATE TYPE dbo.QuotationItemType AS TABLE
                (
                    ProductId INT NOT NULL,
                    Quantity INT NOT NULL,
                    UnitPrice DECIMAL(18, 2) NOT NULL
                )
            ');
        END
        """);

            /*
             * Tạo hoặc cập nhật stored procedure:
             * dbo.sp_Quotation_Create
             */
            migrationBuilder.Sql(
                """
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
        """);

            /*
             * Tạo hoặc cập nhật stored procedure:
             * dbo.sp_Quotation_GetById
             */
            migrationBuilder.Sql(
                """
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
        """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            /*
             * Drop procedure trước vì procedure đang phụ thuộc vào TVP.
             */
            migrationBuilder.Sql(
                """
        IF OBJECT_ID(N'dbo.sp_Quotation_Create', N'P') IS NOT NULL
        BEGIN
            DROP PROCEDURE dbo.sp_Quotation_Create;
        END
        """);

            migrationBuilder.Sql(
                """
        IF OBJECT_ID(N'dbo.sp_Quotation_GetById', N'P') IS NOT NULL
        BEGIN
            DROP PROCEDURE dbo.sp_Quotation_GetById;
        END
        """);

            /*
             * Drop type sau cùng.
             */
            migrationBuilder.Sql(
                """
        IF TYPE_ID(N'dbo.QuotationItemType') IS NOT NULL
        BEGIN
            DROP TYPE dbo.QuotationItemType;
        END
        """);
        }
    }
}
