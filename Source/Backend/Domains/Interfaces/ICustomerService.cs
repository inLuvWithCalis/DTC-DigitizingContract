using ContractManagement.Domains.DTOs.Requests;

namespace ContractManagement.Domains.Interfaces
{
    public interface ICustomerService
    {
        Task<List<CustomerResponseDto>>  GetAllCustomerAsync();
    }
}
