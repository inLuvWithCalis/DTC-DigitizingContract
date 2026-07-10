using ContractManagement.API.Common.Responses;
using ContractManagement.API.Domains.DTOs.Requests.Catalog;
using ContractManagement.API.Domains.Interfaces.Catalog;
using ContractManagement.Domains.DTOs.Responses.Catalog;
using ContractManagement.Infrastructure.Persistence.Application;
using ContractManagement.Infrastructure.Persistence.Application.Models;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.API.Domains.Services.Catalog
{
    /// <summary>
    /// Service xử lý nghiệp vụ loại dịch vụ.
    /// Lưu ý:
    /// - DbDtctechContext đã được resolve theo tenant hiện tại.
    /// - DB không có FK, nên service tự check ServiceType có đang được Service sử dụng hay không.
    /// </summary>
    public class ServiceTypeService : IServiceTypeService
    {
        private readonly DbDtctechContext _dbContext;

        public ServiceTypeService(DbDtctechContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<PagedResult<ServiceTypeResponse>> GetListAsync(
            ServiceTypeFilterRequest filter)
        {
            if (filter.Page <= 0) filter.Page = 1;
            if (filter.PageSize <= 0) filter.PageSize = 20;

            var query = _dbContext.TblServiceTypes
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var keyword = filter.Keyword.Trim();

                query = query.Where(x =>
                    x.ServiceTypeName != null &&
                    x.ServiceTypeName.Contains(keyword));
            }

            if (filter.LangId.HasValue)
            {
                query = query.Where(x => x.LangId == filter.LangId.Value);
            }

            var totalCount = await query.CountAsync();

            var serviceTypes = await query
                .OrderBy(x => x.ServiceTypeId)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var serviceCounts = await GetServiceCountsAsync(serviceTypes);

            return new PagedResult<ServiceTypeResponse>
            {
                Items = serviceTypes
                    .Select(x => MapToResponse(
                        x,
                        serviceCounts.TryGetValue(x.ServiceTypeId, out var count)
                            ? count
                            : 0))
                    .ToList(),

                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }

        public async Task<ServiceTypeResponse> GetByIdAsync(byte id)
        {
            var serviceType = await _dbContext.TblServiceTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ServiceTypeId == id);

            if (serviceType == null)
            {
                throw new KeyNotFoundException("Không tìm thấy loại dịch vụ.");
            }

            var serviceCount = await _dbContext.TblServices
                .AsNoTracking()
                .CountAsync(x => x.ServiceTypeId == id);

            return MapToResponse(serviceType, serviceCount);
        }

        public async Task<ServiceTypeResponse> CreateAsync(
            CreateServiceTypeRequest request)
        {
            var name = request.ServiceTypeName.Trim();

            var nameExists = await _dbContext.TblServiceTypes
                .AnyAsync(x => x.ServiceTypeName == name);

            if (nameExists)
            {
                throw new InvalidOperationException("Tên loại dịch vụ đã tồn tại.");
            }

            var serviceType = new TblServiceType
            {
                ServiceTypeName = name,
                LangId = request.LangId
            };

            _dbContext.TblServiceTypes.Add(serviceType);
            await _dbContext.SaveChangesAsync();

            return MapToResponse(serviceType, 0);
        }

        public async Task UpdateAsync(byte id, UpdateServiceTypeRequest request)
        {
            var serviceType = await _dbContext.TblServiceTypes
                .FirstOrDefaultAsync(x => x.ServiceTypeId == id);

            if (serviceType == null)
            {
                throw new KeyNotFoundException("Không tìm thấy loại dịch vụ.");
            }

            var name = request.ServiceTypeName.Trim();

            var nameExists = await _dbContext.TblServiceTypes
                .AnyAsync(x =>
                    x.ServiceTypeId != id &&
                    x.ServiceTypeName == name);

            if (nameExists)
            {
                throw new InvalidOperationException("Tên loại dịch vụ đã tồn tại.");
            }

            serviceType.ServiceTypeName = name;
            serviceType.LangId = request.LangId;

            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(byte id)
        {
            var serviceType = await _dbContext.TblServiceTypes
                .FirstOrDefaultAsync(x => x.ServiceTypeId == id);

            if (serviceType == null)
            {
                throw new KeyNotFoundException("Không tìm thấy loại dịch vụ.");
            }

            // Không xóa ServiceType nếu đã có Service sử dụng.
            // Vì DB không có FK nên phải tự check bằng code.
            var hasServices = await _dbContext.TblServices
                .AnyAsync(x => x.ServiceTypeId == id);

            if (hasServices)
            {
                throw new InvalidOperationException(
                    "Không thể xóa loại dịch vụ vì đang có dịch vụ sử dụng.");
            }

            _dbContext.TblServiceTypes.Remove(serviceType);
            await _dbContext.SaveChangesAsync();
        }

        private async Task<Dictionary<byte, int>> GetServiceCountsAsync(
            List<TblServiceType> serviceTypes)
        {
            var serviceTypeIds = serviceTypes
                .Select(x => x.ServiceTypeId)
                .ToList();

            if (serviceTypeIds.Count == 0)
            {
                return new Dictionary<byte, int>();
            }

            return await _dbContext.TblServices
                .AsNoTracking()
                .Where(x =>
                    x.ServiceTypeId.HasValue &&
                    serviceTypeIds.Contains(x.ServiceTypeId.Value))
                .GroupBy(x => x.ServiceTypeId!.Value)
                .ToDictionaryAsync(
                    x => x.Key,
                    x => x.Count());
        }

        private static ServiceTypeResponse MapToResponse(
            TblServiceType serviceType,
            int serviceCount)
        {
            return new ServiceTypeResponse
            {
                ServiceTypeId = serviceType.ServiceTypeId,
                ServiceTypeName = serviceType.ServiceTypeName,
                LangId = serviceType.LangId,
                ServiceCount = serviceCount
            };
        }
    }
}