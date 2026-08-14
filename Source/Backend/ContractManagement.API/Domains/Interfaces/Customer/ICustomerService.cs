using ContractManagement.API.Common.Responses;
using ContractManagement.API.Domains.DTOs.Requests.Customer;
using ContractManagement.API.Domains.DTOs.Responses.Customer;

namespace ContractManagement.API.Domains.Interfaces.Customer
{
    /// <summary>
    /// Service quản lý khách hàng.
    /// Khách hàng sẽ được gắn với hợp đồng qua CustomerId.
    /// </summary>
    public interface ICustomerService
    {
        Task<PagedResult<CustomerResponse>> GetListAsync(CustomerFilterRequest filter);

        Task<IReadOnlyList<CustomerLookupResponse>> GetLookupAsync(
            string? keyword,
            CancellationToken cancellationToken = default);

        Task<CustomerResponse> GetByIdAsync(int id);

        Task<CustomerResponse> CreateAsync(CreateCustomerRequest request, int createdBy);

        Task UpdateAsync(int id, UpdateCustomerRequest request, int updatedBy);

        Task SetStatusAsync(int id, byte status);
    }
}
