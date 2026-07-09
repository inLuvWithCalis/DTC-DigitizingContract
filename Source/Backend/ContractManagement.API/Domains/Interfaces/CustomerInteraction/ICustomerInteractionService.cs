using ContractManagement.API.Domains.DTOs.Requests.CustomerInteraction;
using ContractManagement.API.Domains.DTOs.Responses.CustomerInteraction;

namespace ContractManagement.API.Domains.Interfaces.CustomerInteraction
{
    public interface ICustomerInteractionService
    {
        Task<CustomerInteractionResponse> CreateAsync(
            int customerId,
            CreateCustomerInteractionRequest request,
            int employeeId);

        Task<List<CustomerInteractionResponse>> GetByCustomerAsync(int customerId);

        Task UpdateAsync(
            int customerId,
            int interactionId,
            UpdateCustomerInteractionRequest request);
    }
}