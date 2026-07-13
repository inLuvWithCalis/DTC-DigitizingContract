using ContractManagement.API.Common.Responses;
using ContractManagement.API.Domains.DTOs.Requests.Catalog;
using ContractManagement.API.Domains.DTOs.Responses.Catalog;
using ContractManagement.API.Domains.Interfaces.Catalog;
using ContractManagement.Infrastructure.Persistence.Application;
using ContractManagement.Infrastructure.Persistence.Application.Models;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Domains.Services.Catalog
{
    /// <summary>
    /// Service xử lý nghiệp vụ dịch vụ.
    /// - DbDtctechContext đã trỏ đúng tenant DB.
    /// - DB không dùng FK, nên service tự check ServiceType/ParentService.
    /// </summary>
    public class ServiceService : IServiceService
    {
        private readonly DbDtctechContext _dbContext;

        public ServiceService(DbDtctechContext dbContext)
        {
            _dbContext = dbContext;
        }

        private IQueryable<TblService> ApplyDateFilter(IQueryable<TblService> query, DateTime? startDate, DateTime? endDate)
        {
            if (!startDate.HasValue && !endDate.HasValue)
            {
                return query;
            }

            var from = startDate?.Date ?? DateTime.MinValue;
            var to = endDate?.Date.AddDays(1) ?? DateTime.MaxValue;

            return query.Where(x => x.DateCreated >= from && x.DateCreated < to);
        }

        public async Task<PagedResult<ServiceResponse>> GetListAsync(ServiceFilterRequest filter)
        {
            if (filter.Page <= 0) filter.Page = 1;
            if (filter.PageSize <= 0) filter.PageSize = 20;

            var query = _dbContext.TblServices
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var keyword = filter.Keyword.Trim();

                query = query.Where(x =>
                    (x.ServiceName != null && x.ServiceName.Contains(keyword)) ||
                    (x.ServiceShortDesc != null && x.ServiceShortDesc.Contains(keyword)));
            }

            if (filter.ServiceTypeId.HasValue)
            {
                query = query.Where(x => x.ServiceTypeId == filter.ServiceTypeId.Value);
            }

            if (filter.Status.HasValue)
            {
                query = query.Where(x => x.Status == filter.Status.Value);
            }

            if (filter.LangId.HasValue)
            {
                query = query.Where(x => x.LangId == filter.LangId.Value);
            }

            query = ApplyDateFilter(query, filter.FromDate, filter.ToDate);

            var totalCount = await query.CountAsync();

            var services = await query
                .OrderByDescending(x => x.ServiceId)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var serviceTypeNames = await GetServiceTypeNamesAsync(services);

            return new PagedResult<ServiceResponse>
            {
                Items = services
                    .Select(x => MapToResponse(
                        x,
                        x.ServiceTypeId.HasValue &&
                        serviceTypeNames.TryGetValue(x.ServiceTypeId.Value, out var typeName)
                            ? typeName
                            : null))
                    .ToList(),

                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }

        public async Task<ServiceResponse> GetByIdAsync(int id)
        {
            var service = await _dbContext.TblServices
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ServiceId == id);

            if (service == null)
            {
                throw new KeyNotFoundException("Không tìm thấy dịch vụ.");
            }

            string? serviceTypeName = null;

            if (service.ServiceTypeId.HasValue)
            {
                serviceTypeName = await _dbContext.TblServiceTypes
                    .AsNoTracking()
                    .Where(x => x.ServiceTypeId == service.ServiceTypeId.Value)
                    .Select(x => x.ServiceTypeName)
                    .FirstOrDefaultAsync();
            }

            return MapToResponse(service, serviceTypeName);
        }

        public async Task<ServiceResponse> CreateAsync(
            CreateServiceRequest request,
            int createdBy)
        {
            await ValidateServiceTypeAsync(request.ServiceTypeId);
            await ValidateParentServiceAsync(request.ServiceParentId, currentServiceId: null);

            var service = new TblService
            {
                ServiceName = request.ServiceName.Trim(),
                ServiceTypeId = request.ServiceTypeId,
                ServiceParentId = request.ServiceParentId,
                ServicePrice = request.ServicePrice,
                SetupPrice = request.SetupPrice,
                MaintainPrice = request.MaintainPrice,
                LangId = request.LangId,
                ServiceImageIcon = request.ServiceImageIcon?.Trim(),
                ServiceShortDesc = request.ServiceShortDesc?.Trim(),
                ServiceContent = request.ServiceContent?.Trim(),
                ServiceOrder = request.ServiceOrder,
                ServiceRegion = request.ServiceRegion,
                Rewrite = request.Rewrite?.Trim(),
                TitleBrowser = request.TitleBrowser?.Trim(),
                MetaKeyword = request.MetaKeyword?.Trim(),
                MetaDescription = request.MetaDescription?.Trim(),
                Others = request.Others?.Trim(),

                // Quy ước: 1 = Active, 0 = Inactive
                Status = 1,

                UserCreated = createdBy,
                DateCreated = DateTime.Now,

                // Mặc định service mới chưa có service con.
                HasChild = false
            };

            _dbContext.TblServices.Add(service);
            await _dbContext.SaveChangesAsync();

            await UpdateParentHasChildAsync(service.ServiceParentId);

            return await GetByIdAsync(service.ServiceId);
        }

        public async Task UpdateAsync(
            int id,
            UpdateServiceRequest request,
            int updatedBy)
        {
            var service = await _dbContext.TblServices
                .FirstOrDefaultAsync(x => x.ServiceId == id);

            if (service == null)
            {
                throw new KeyNotFoundException("Không tìm thấy dịch vụ.");
            }

            await ValidateServiceTypeAsync(request.ServiceTypeId);
            await ValidateParentServiceAsync(request.ServiceParentId, currentServiceId: id);

            var oldParentId = service.ServiceParentId;

            service.ServiceName = request.ServiceName.Trim();
            service.ServiceTypeId = request.ServiceTypeId;
            service.ServiceParentId = request.ServiceParentId;
            service.ServicePrice = request.ServicePrice;
            service.SetupPrice = request.SetupPrice;
            service.MaintainPrice = request.MaintainPrice;
            service.LangId = request.LangId;
            service.ServiceImageIcon = request.ServiceImageIcon?.Trim();
            service.ServiceShortDesc = request.ServiceShortDesc?.Trim();
            service.ServiceContent = request.ServiceContent?.Trim();
            service.ServiceOrder = request.ServiceOrder;
            service.ServiceRegion = request.ServiceRegion;
            service.Rewrite = request.Rewrite?.Trim();
            service.TitleBrowser = request.TitleBrowser?.Trim();
            service.MetaKeyword = request.MetaKeyword?.Trim();
            service.MetaDescription = request.MetaDescription?.Trim();
            service.Others = request.Others?.Trim();

            service.UserModified = updatedBy;
            service.DateModified = DateTime.Now;

            await _dbContext.SaveChangesAsync();

            await UpdateParentHasChildAsync(oldParentId);
            await UpdateParentHasChildAsync(service.ServiceParentId);
        }

        public async Task SetStatusAsync(int id, byte status, int updatedBy)
        {
            if (status is not 0 and not 1)
            {
                throw new ArgumentException("Trạng thái dịch vụ không hợp lệ. Chỉ nhận 0 hoặc 1.");
            }

            var service = await _dbContext.TblServices
                .FirstOrDefaultAsync(x => x.ServiceId == id);

            if (service == null)
            {
                throw new KeyNotFoundException("Không tìm thấy dịch vụ.");
            }

            service.Status = status;
            service.UserModified = updatedBy;
            service.DateModified = DateTime.Now;

            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id, int updatedBy)
        {
            var service = await _dbContext.TblServices
                .FirstOrDefaultAsync(x => x.ServiceId == id);

            if (service == null)
            {
                throw new KeyNotFoundException("Không tìm thấy dịch vụ.");
            }

            // Không hard delete vì service có thể được dùng trong báo giá/hợp đồng sau này.
            // Xóa ở đây hiểu là ẩn/ngưng sử dụng.
            service.Status = 0;
            service.UserModified = updatedBy;
            service.DateModified = DateTime.Now;

            await _dbContext.SaveChangesAsync();
        }

        private async Task ValidateServiceTypeAsync(byte? serviceTypeId)
        {
            if (!serviceTypeId.HasValue)
            {
                return;
            }

            var exists = await _dbContext.TblServiceTypes
                .AnyAsync(x => x.ServiceTypeId == serviceTypeId.Value);

            if (!exists)
            {
                throw new KeyNotFoundException("Loại dịch vụ không tồn tại.");
            }
        }

        private async Task ValidateParentServiceAsync(
            int? parentServiceId,
            int? currentServiceId)
        {
            if (!parentServiceId.HasValue)
            {
                return;
            }

            if (currentServiceId.HasValue && parentServiceId.Value == currentServiceId.Value)
            {
                throw new InvalidOperationException("Dịch vụ không thể chọn chính nó làm cha.");
            }

            var exists = await _dbContext.TblServices
                .AnyAsync(x => x.ServiceId == parentServiceId.Value);

            if (!exists)
            {
                throw new KeyNotFoundException("Dịch vụ cha không tồn tại.");
            }
        }

        private async Task UpdateParentHasChildAsync(int? parentServiceId)
        {
            if (!parentServiceId.HasValue)
            {
                return;
            }

            var parent = await _dbContext.TblServices
                .FirstOrDefaultAsync(x => x.ServiceId == parentServiceId.Value);

            if (parent == null)
            {
                return;
            }

            parent.HasChild = await _dbContext.TblServices
                .AnyAsync(x => x.ServiceParentId == parentServiceId.Value);

            await _dbContext.SaveChangesAsync();
        }

        private async Task<Dictionary<byte, string>> GetServiceTypeNamesAsync(
            List<TblService> services)
        {
            var serviceTypeIds = services
                .Where(x => x.ServiceTypeId.HasValue)
                .Select(x => x.ServiceTypeId!.Value)
                .Distinct()
                .ToList();

            if (serviceTypeIds.Count == 0)
            {
                return new Dictionary<byte, string>();
            }

            return await _dbContext.TblServiceTypes
                .AsNoTracking()
                .Where(x => serviceTypeIds.Contains(x.ServiceTypeId))
                .ToDictionaryAsync(
                    x => x.ServiceTypeId,
                    x => x.ServiceTypeName ?? string.Empty);
        }

        private static ServiceResponse MapToResponse(
            TblService service,
            string? serviceTypeName)
        {
            return new ServiceResponse
            {
                ServiceId = service.ServiceId,
                ServiceName = service.ServiceName,
                ServiceTypeId = service.ServiceTypeId,
                ServiceTypeName = serviceTypeName,
                ServiceParentId = service.ServiceParentId,
                ServicePrice = service.ServicePrice,
                SetupPrice = service.SetupPrice,
                MaintainPrice = service.MaintainPrice,
                Status = service.Status,
                LangId = service.LangId,
                ServiceImageIcon = service.ServiceImageIcon,
                ServiceShortDesc = service.ServiceShortDesc,
                ServiceContent = service.ServiceContent,
                ServiceOrder = service.ServiceOrder,
                ServiceRegion = service.ServiceRegion,
                Rewrite = service.Rewrite,
                TitleBrowser = service.TitleBrowser,
                MetaKeyword = service.MetaKeyword,
                MetaDescription = service.MetaDescription,
                Others = service.Others,
                UserCreated = service.UserCreated,
                UserModified = service.UserModified,
                DateCreated = service.DateCreated,
                DateModified = service.DateModified
            };
        }
    }
}