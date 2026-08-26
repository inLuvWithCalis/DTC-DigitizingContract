using ContractManagement.API.Common.Responses;
using ContractManagement.API.Domains.DTOs.Requests.Customer;
using ContractManagement.API.Domains.DTOs.Responses.Customer;
using ContractManagement.API.Domains.Interfaces.Customer;
using ContractManagement.Infrastructure.Persistence.Application;
using ContractManagement.Infrastructure.Persistence.Application.Models;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.API.Domains.Services.Customer
{
    /// <summary>
    /// Service xử lý nghiệp vụ khách hàng.
    /// - DbDtctechContext đã trỏ đúng tenant DB hiện tại.
    /// - Không dùng DefaultConnection.
    /// - Không trả CustomerPassword ra ngoài API.
    /// </summary>
    public class CustomerService : ICustomerService
    {
        private readonly DbDtctechContext _dbContext;

        public CustomerService(DbDtctechContext dbContext)
        {
            _dbContext = dbContext;
        }

        private IQueryable<TblCustomer> ApplyDateFilter(IQueryable<TblCustomer> query, DateTime? fromDate, DateTime? toDate)
        {
            if (!fromDate.HasValue && !toDate.HasValue) return query;

            var from = fromDate?.Date ?? DateTime.MinValue;
            var to = toDate?.Date.AddDays(1) ?? DateTime.MaxValue;

            return query.Where(x => x.DateCreated >= from && x.DateCreated < to);
        }

        public async Task<PagedResult<CustomerResponse>> GetListAsync(
            CustomerFilterRequest filter)
        {
            if (filter.Page <= 0) filter.Page = 1;
            if (filter.PageSize <= 0) filter.PageSize = 20;

            var query = _dbContext.TblCustomers.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var keyword = filter.Keyword.Trim();

                query = query.Where(x =>
                    (x.CustomerFullName != null && x.CustomerFullName.Contains(keyword)) ||
                    (x.CustomerCompany != null && x.CustomerCompany.Contains(keyword)) ||
                    (x.CustomerEmail != null && x.CustomerEmail.Contains(keyword)) ||
                    (x.CustomerMobile != null && x.CustomerMobile.Contains(keyword)) ||
                    (x.CustomerTaxCode != null && x.CustomerTaxCode.Contains(keyword)));
            }

            if (filter.Status.HasValue)
            {
                query = query.Where(x => x.Status == filter.Status.Value);
            }

            query = ApplyDateFilter(query, filter.FromDate, filter.ToDate);

            var totalCount = await query.CountAsync();

            var customers = await query
                .OrderByDescending(x => x.CustomerId)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var contractCounts = await GetContractCountsAsync(customers);

            return new PagedResult<CustomerResponse>
            {
                Items = customers
                    .Select(x => MapToResponse(
                        x,
                        contractCounts.TryGetValue(x.CustomerId, out var count)
                            ? count
                            : 0))
                    .ToList(),

                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }

        public async Task<IReadOnlyList<CustomerLookupResponse>> GetLookupAsync(
            string? keyword,
            CancellationToken cancellationToken = default)
        {
            var query = _dbContext.TblCustomers.AsNoTracking();
            var normalizedKeyword = keyword?.Trim();

            if (!string.IsNullOrWhiteSpace(normalizedKeyword))
            {
                query = query.Where(x =>
                    (x.CustomerCode != null && x.CustomerCode.Contains(normalizedKeyword))
                    || (x.CustomerFullName != null && x.CustomerFullName.Contains(normalizedKeyword))
                    || (x.CustomerCompany != null && x.CustomerCompany.Contains(normalizedKeyword))
                    || (x.CustomerTaxCode != null && x.CustomerTaxCode.Contains(normalizedKeyword))
                    || (x.CustomerMobile != null && x.CustomerMobile.Contains(normalizedKeyword))
                    || (x.CustomerPhone != null && x.CustomerPhone.Contains(normalizedKeyword)));
            }

            return await query
                .OrderBy(x => x.CustomerFullName ?? x.CustomerCompany ?? x.CustomerCode)
                .ThenBy(x => x.CustomerId)
                .Take(100)
                .Select(x => new CustomerLookupResponse
                {
                    CustomerId = x.CustomerId,
                    CustomerCode = x.CustomerCode,
                    CustomerFullName = x.CustomerFullName,
                    CustomerCompany = x.CustomerCompany,
                    CustomerTaxCode = x.CustomerTaxCode,
                    CustomerMobile = x.CustomerMobile,
                    CustomerPhone = x.CustomerPhone,
                    Status = x.Status
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<CustomerResponse> GetByIdAsync(int id)
        {
            var customer = await _dbContext.TblCustomers
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.CustomerId == id);

            if (customer == null)
            {
                throw new KeyNotFoundException("Không tìm thấy khách hàng.");
            }

            var totalContracts = await _dbContext.TblContracts
                .AsNoTracking()
                .CountAsync(x => x.CustomerId == id);

            return MapToResponse(customer, totalContracts);
        }

        public async Task<CustomerResponse> CreateAsync(
            CreateCustomerRequest request,
            int createdBy)
        {
            // Check trùng mã khách hàng nếu có nhập CustomerCode
            if (!string.IsNullOrWhiteSpace(request.CustomerCode))
            {
                var code = request.CustomerCode.Trim();

                var codeExists = await _dbContext.TblCustomers
                    .AnyAsync(x => x.CustomerCode == code);

                if (codeExists)
                {
                    throw new InvalidOperationException("Mã khách hàng đã tồn tại.");
                }
            }

            var customer = new TblCustomer
            {
                CustomerCode = request.CustomerCode?.Trim(),
                CustomerFullName = request.CustomerFullName.Trim(),
                CustomerCompany = request.CustomerCompany?.Trim(),
                CustomerEmail = request.CustomerEmail?.Trim(),
                CustomerMobile = request.CustomerMobile?.Trim(),
                CustomerPhone = request.CustomerPhone?.Trim(),
                CustomerFaxNumber = request.CustomerFaxNumber?.Trim(),
                CustomerTaxCode = request.CustomerTaxCode?.Trim(),
                CustomerRepresentativeName = request.CustomerRepresentativeName?.Trim(),
                CustomerRepresentativeTitle = request.CustomerRepresentativeTitle?.Trim(),
                CustomerBankAccountNumber = request.CustomerBankAccountNumber?.Trim(),
                CustomerBankName = request.CustomerBankName?.Trim(),
                CustomerAddress = request.CustomerAddress?.Trim(),
                CustomerCity = request.CustomerCity?.Trim(),
                CustomerCountry = request.CustomerCountry?.Trim(),
                CustomerWebsite = request.CustomerWebsite?.Trim(),
                CustomerNotes = request.CustomerNotes?.Trim(),

                UserCreated = createdBy,
                DateCreated = DateTime.Now,

                // Quy ước hệ thống mới: 1 = Active, 0 = Inactive
                Status = 1
            };

            _dbContext.TblCustomers.Add(customer);
            await _dbContext.SaveChangesAsync();

            return MapToResponse(customer, 0);
        }

        public async Task UpdateAsync(
            int id,
            UpdateCustomerRequest request,
            int updatedBy)
        {
            var customer = await _dbContext.TblCustomers
                .FirstOrDefaultAsync(x => x.CustomerId == id);

            if (customer == null)
            {
                throw new KeyNotFoundException("Không tìm thấy khách hàng.");
            }

            // Nếu đổi CustomerCode thì check trùng với khách khác
            if (!string.IsNullOrWhiteSpace(request.CustomerCode))
            {
                var code = request.CustomerCode.Trim();

                var codeExists = await _dbContext.TblCustomers
                    .AnyAsync(x =>
                        x.CustomerId != id &&
                        x.CustomerCode == code);

                if (codeExists)
                {
                    throw new InvalidOperationException("Mã khách hàng đã tồn tại.");
                }
            }

            customer.CustomerCode = request.CustomerCode?.Trim();
            customer.CustomerFullName = request.CustomerFullName.Trim();
            customer.CustomerCompany = request.CustomerCompany?.Trim();
            customer.CustomerEmail = request.CustomerEmail?.Trim();
            customer.CustomerMobile = request.CustomerMobile?.Trim();
            customer.CustomerPhone = request.CustomerPhone?.Trim();
            customer.CustomerFaxNumber = request.CustomerFaxNumber?.Trim();
            customer.CustomerTaxCode = request.CustomerTaxCode?.Trim();
            customer.CustomerRepresentativeName = request.CustomerRepresentativeName?.Trim();
            customer.CustomerRepresentativeTitle = request.CustomerRepresentativeTitle?.Trim();
            customer.CustomerBankAccountNumber = request.CustomerBankAccountNumber?.Trim();
            customer.CustomerBankName = request.CustomerBankName?.Trim();
            customer.CustomerAddress = request.CustomerAddress?.Trim();
            customer.CustomerCity = request.CustomerCity?.Trim();
            customer.CustomerCountry = request.CustomerCountry?.Trim();
            customer.CustomerWebsite = request.CustomerWebsite?.Trim();
            customer.CustomerNotes = request.CustomerNotes?.Trim();

            customer.UserModified = updatedBy;
            customer.DateModified = DateTime.Now;

            await _dbContext.SaveChangesAsync();
        }

        public async Task SetStatusAsync(int id, byte status)
        {
            if (status is not 0 and not 1)
            {
                throw new ArgumentException("Trạng thái khách hàng không hợp lệ. Chỉ nhận 0 hoặc 1.");
            }

            var customer = await _dbContext.TblCustomers
                .FirstOrDefaultAsync(x => x.CustomerId == id);

            if (customer == null)
            {
                throw new KeyNotFoundException("Không tìm thấy khách hàng.");
            }

            customer.Status = status;
            customer.DateModified = DateTime.Now;

            await _dbContext.SaveChangesAsync();
        }

        private async Task<Dictionary<int, int>> GetContractCountsAsync(
            List<TblCustomer> customers)
        {
            var customerIds = customers
                .Select(x => x.CustomerId)
                .ToList();

            if (customerIds.Count == 0)
            {
                return new Dictionary<int, int>();
            }

            return await _dbContext.TblContracts
                .AsNoTracking()

                /*
                 * TblContract.CustomerId hiện là int bắt buộc,
                 * nên không cần HasValue hoặc Value nữa.
                 */
                .Where(x => customerIds.Contains(x.CustomerId))
                .GroupBy(x => x.CustomerId)
                .ToDictionaryAsync(
                    x => x.Key,
                    x => x.Count());
        }

        private static CustomerResponse MapToResponse(
            TblCustomer customer,
            int totalContracts)
        {
            return new CustomerResponse
            {
                CustomerId = customer.CustomerId,
                CustomerCode = customer.CustomerCode,
                CustomerFullName = customer.CustomerFullName,
                CustomerCompany = customer.CustomerCompany,
                CustomerEmail = customer.CustomerEmail,
                CustomerMobile = customer.CustomerMobile,
                CustomerPhone = customer.CustomerPhone,
                CustomerFaxNumber = customer.CustomerFaxNumber,
                CustomerTaxCode = customer.CustomerTaxCode,
                CustomerRepresentativeName = customer.CustomerRepresentativeName,
                CustomerRepresentativeTitle = customer.CustomerRepresentativeTitle,
                CustomerBankAccountNumber = customer.CustomerBankAccountNumber,
                CustomerBankName = customer.CustomerBankName,
                CustomerAddress = customer.CustomerAddress,
                CustomerCity = customer.CustomerCity,
                CustomerCountry = customer.CustomerCountry,
                CustomerWebsite = customer.CustomerWebsite,
                CustomerNotes = customer.CustomerNotes,
                Status = customer.Status,
                DateCreated = customer.DateCreated,
                DateModified = customer.DateModified,
                TotalContracts = totalContracts
            };
        }
    }
}
