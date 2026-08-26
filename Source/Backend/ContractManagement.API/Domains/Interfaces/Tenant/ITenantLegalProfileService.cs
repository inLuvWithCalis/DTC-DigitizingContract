using ContractManagement.API.Domains.DTOs.Requests.LegalProfiles;
using ContractManagement.API.Domains.DTOs.Responses.LegalProfiles;

namespace ContractManagement.API.Domains.Interfaces.LegalProfiles;

public interface ITenantLegalProfileService
{
    Task<TenantLegalProfileResponse?> GetAsync(
        CancellationToken cancellationToken = default);

    Task<TenantLegalProfileResponse> UpsertAsync(
        UpsertTenantLegalProfileRequest request,
        int employeeId,
        CancellationToken cancellationToken = default);
}
