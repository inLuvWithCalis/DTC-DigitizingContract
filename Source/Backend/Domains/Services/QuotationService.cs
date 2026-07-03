using AutoMapper;
using ContractManagement.Data;
using ContractManagement.Domains.DTOs.Requests;
using ContractManagement.Domains.DTOs.Responses;
using ContractManagement.Domains.Interfaces;
using ContractManagement.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace ContractManagement.Domains.Services
{
    public class QuotationService : IQuotationService
    {
        private readonly DbDtctechContext _dbDtctechContext;

        // AutoMapper is used to simplify the mapping between DTOs and Entity models.
        private readonly IMapper _mapper;

        private readonly string _connectionString;
        public QuotationService(DbDtctechContext dbDtctechContext, IMapper mapper, IConfiguration configuration)
        {
            _dbDtctechContext = dbDtctechContext;
            _mapper = mapper;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<QuotationResponseDto> CreateQuotationAsync(
            CreateQuotationRequestDto request,
            int currentEmployeeId)
        {
            DataTable quotationItemsTable =
                CreateQuotationItemsTable(request.QuotationItems);

            await using var connection =
                new SqlConnection(_connectionString);

            await connection.OpenAsync();

            await using var command =
                CreateQuotationCommand(
                    connection,
                    request.CustomerId,
                    currentEmployeeId,
                    quotationItemsTable);

            object? result = await command.ExecuteScalarAsync();

            if (result is null || result == DBNull.Value)
            {
                throw new InvalidOperationException(
                    "Failed to create quotation.");
            }

            int quotationId = Convert.ToInt32(result);

            return await GetQuotationByIdAsync(quotationId);
        }

        private static DataTable CreateQuotationItemsTable(
            List<QuotationItemDto> quotationItems)
        {
            var table = new DataTable();

            table.Columns.Add("ProductId", typeof(int));
            table.Columns.Add("Quantity", typeof(int));
            table.Columns.Add("UnitPrice", typeof(decimal));

            foreach (var item in quotationItems)
            {
                table.Rows.Add(
                    item.ProductId,
                    item.Quantity,
                    Convert.ToDecimal(item.UnitPrice));
            }

            return table;
        }

        private static SqlCommand CreateQuotationCommand(
            SqlConnection connection,
            int customerId,
            int currentEmployeeId,
            DataTable quotationItemsTable)
        {
            var command = new SqlCommand(
                "dbo.sp_Quotation_Create",
                connection);

            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add(
                new SqlParameter("@CustomerId", SqlDbType.Int)
                {
                    Value = customerId
                });

            command.Parameters.Add(
                new SqlParameter("@CreatedEmployeeId", SqlDbType.Int)
                {
                    Value = currentEmployeeId
                });

            command.Parameters.Add(
                new SqlParameter("@Items", SqlDbType.Structured)
                {
                    TypeName = "dbo.QuotationItemType",
                    Value = quotationItemsTable
                });

            return command;
        }

        public async Task<bool> DeleteQuotationAsync(int quotationId)
        {
            // 1. Lấy thông tin báo giá từ database.
            var quotation = _dbDtctechContext.TblQuotations.FirstOrDefault(q => q.QuotationId == quotationId);

            // 2. Nếu không tìm thấy báo giá, trả về lỗi.
            if (quotation == null)
            {
                throw new Exception("Cannot find quotation with the given ID.");
            }

            // 2.1 Chi cho phep xoa bao gia Draft
            if (quotation.QuatationStatus != "Draft")
            {
                throw new Exception("Only quotations with 'Draft' status can be deleted.");
            }

            // 3. Xóa báo giá và chi tiết báo giá.
            using var transaction = await _dbDtctechContext.Database.BeginTransactionAsync();
            try
            {
                // 3.1 Xóa chi tiết báo giá
                var quotationDetails = _dbDtctechContext.TblQuotationDetails.Where(d => d.QuotationId == quotationId);
                _dbDtctechContext.TblQuotationDetails.RemoveRange(quotationDetails);

                // 3.2 Xóa báo giá
                _dbDtctechContext.TblQuotations.Remove(quotation);

                // 3.3 Lưu thay đổi vào database.
                await _dbDtctechContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;

            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception("Error deleting quotation: " + ex.Message);
            }
        }

        public async Task<List<QuotationResponseDto>> GetAllQuotationsAsync()
        {
            var quotations = await _dbDtctechContext.TblQuotations.OrderByDescending(q => q.QuotationDate).AsNoTracking().ToListAsync();
            return _mapper.Map<List<QuotationResponseDto>>(quotations);
        }

        public async Task<QuotationResponseDto> GetQuotationByIdAsync(
            int quotationId)
        {
            await using var connection = new SqlConnection(_connectionString);

            await connection.OpenAsync();

            Console.WriteLine($"Database: {connection.Database}");
            Console.WriteLine($"Server: {connection.DataSource}");

            await using var command = new SqlCommand(
                "dbo.sp_Quotation_GetById",
                connection);

            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add(
                new SqlParameter("@QuotationId", SqlDbType.Int)
                {
                    Value = quotationId
                });

            await using var reader =
                await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                throw new KeyNotFoundException(
                    $"Quotation {quotationId} was not found.");
            }

            var quotation = new QuotationResponseDto
            {
                QuotationId = reader.GetInt32(
                    reader.GetOrdinal("QuotationId")),

                QuotationNo = reader.GetString(
                    reader.GetOrdinal("QuotationNo")),

                CustomerId = reader.GetInt32(
                    reader.GetOrdinal("CustomerId")),

                QuotationDate = reader.GetDateTime(
                    reader.GetOrdinal("QuotationDate")),

                TotalAmount = reader.GetDecimal(
                    reader.GetOrdinal("TotalAmount")),

                QuatationStatus =
                    reader.IsDBNull(
                        reader.GetOrdinal("QuatationStatus"))
                        ? null
                        : reader.GetString(
                            reader.GetOrdinal("QuatationStatus"))
            };

            await reader.NextResultAsync();

            while (await reader.ReadAsync())
            {
                quotation.Items.Add(
                    new QuotationResponseDto.ItemResponse
                    {
                        ProductId = reader.GetInt32(
                            reader.GetOrdinal("ProductId")),

                        ProductName =
                            reader.IsDBNull(
                                reader.GetOrdinal("ProductName"))
                                ? string.Empty
                                : reader.GetString(
                                    reader.GetOrdinal("ProductName")),

                        Quantity =
                            reader.IsDBNull(
                                reader.GetOrdinal("Quantity"))
                                ? 0
                                : reader.GetInt32(
                                    reader.GetOrdinal("Quantity")),

                        UnitPrice =
                            reader.IsDBNull(
                                reader.GetOrdinal("UnitPrice"))
                                ? 0
                                : reader.GetDecimal(
                                    reader.GetOrdinal("UnitPrice")),

                        Amount =
                            reader.IsDBNull(
                                reader.GetOrdinal("Amount"))
                                ? 0
                                : reader.GetDecimal(
                                    reader.GetOrdinal("Amount"))
                    });
            }

            return quotation;
        }

        public async Task<bool> UpdateQuotationAsync(int quotationId, UpdateQuotationRequestDto request)
        {
            // 1. Lấy thông tin báo giá từ database.
            var quotation = _dbDtctechContext.TblQuotations.FirstOrDefault(q => q.QuotationId == quotationId);

            // 2. Nếu không tìm thấy báo giá, trả về lỗi.
            if (quotation == null)
            {
                throw new Exception("Cannot find quotation with the given ID.");
            }

            // 3. Cập nhật thông tin báo giá.
            quotation.QuatationStatus = request.QuotationStatus;
            _dbDtctechContext.TblQuotations.Update(quotation);
            await _dbDtctechContext.SaveChangesAsync();

            // 4. Return true nếu cập nhật thành công.
            return true;
        }
    }
}