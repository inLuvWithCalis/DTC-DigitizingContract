using AutoMapper;
using ContractManagement.Data;
using ContractManagement.Domains.Quotation.DTOs.Requests;
using ContractManagement.Domains.Quotation.DTOs.Responses;
using ContractManagement.Domains.Quotation.Interfaces;
using ContractManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Domains.Quotation.Services
{
    public class QuotationService : IQuotationService
    {
        private readonly DbDtctechContext _dbDtctechContext;

        // AutoMapper is used to simplify the mapping between DTOs and Entity models.
        private readonly IMapper _mapper;

        public QuotationService(DbDtctechContext dbDtctechContext, IMapper mapper)
        {
            _dbDtctechContext = dbDtctechContext;
            _mapper = mapper;
        }

        public async Task<QuotationResponseDto> CreateQuotationAsync(CreateQuotationRequestDto request, int currentEmployeeId)
        {
            // 1. Tính toán tổng tiền báo giá từ Items gửi lên trong request.
            double totalAmount = 0;
            var detailEnitties = new List<TblQuotationDetail>();

            foreach (var item in request.QuotationItems)
            {
                double amount = item.Quantity * item.UnitPrice;
                totalAmount += amount;

                var detailItem = _mapper.Map<TblQuotationDetail>(item);
                detailItem.Amount = amount;
                detailEnitties.Add(detailItem);
            }

            // 2. Mapping sang entity TblQuotation.
            var quotationEntity = _mapper.Map<TblQuotation>(request);
            quotationEntity.QuotationDate = DateTime.Now;
            quotationEntity.TotalAmount = totalAmount;
            quotationEntity.QuatationStatus = "Draft";
            quotationEntity.CreatedEmployeeId = currentEmployeeId;
            quotationEntity.QuotationNo = $"QT-{DateTime.Now:yyyyMMddHHmmss}"; // Customize

            // 3. Open transaction và lưu vào database.
            using var transaction = await _dbDtctechContext.Database.BeginTransactionAsync();
            try
            {
                // 3.1. Save quotationEntity to database.
                await _dbDtctechContext.TblQuotations.AddAsync(quotationEntity);
                await _dbDtctechContext.SaveChangesAsync();

                // 3.2. Save detailEnitties to database with the generated QuotationId.
                foreach (var items in detailEnitties)
                {
                    items.QuotationId = quotationEntity.QuotationId;
                }
                await _dbDtctechContext.TblQuotationDetails.AddRangeAsync(detailEnitties);
                await _dbDtctechContext.SaveChangesAsync();

                // 3.3. Commit transaction nếu tất cả thành công.
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception($"Error creating quotation: {ex.Message}");
            }

            // 4. Mapping sang QuotationResponseDto và return về client.
            var response = _mapper.Map<QuotationResponseDto>(quotationEntity);
            response.Items = _mapper.Map<List<QuotationResponseDto.ItemResponse>>(detailEnitties);

            return response;
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

        /* 
         */
        public async Task<QuotationResponseDto> GetQuotationByIdAsync(int quotationId)
        {
            // 1. Lấy thông tin báo giá từ database.
            var quotation = await _dbDtctechContext.TblQuotations
                .FirstOrDefaultAsync(q => q.QuotationId == quotationId);

            // 2. Nếu không tìm thấy báo giá, trả về lỗi.
            if (quotation == null) { throw new Exception("Cannot find quotation with the given ID."); }

            // 3. Lấy chi tiết báo giá từ database.
            var quotationDetails = await _dbDtctechContext.TblQuotationDetails
                                                          .Where(d => d.QuotationId == quotationId)
                                                          .ToListAsync();

            // 4. Mapping sang QuotationResponseDto và return về client.
            var response = _mapper.Map<QuotationResponseDto>(quotation);
            response.Items = _mapper.Map<List<QuotationResponseDto.ItemResponse>>(quotationDetails);

            return response;
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