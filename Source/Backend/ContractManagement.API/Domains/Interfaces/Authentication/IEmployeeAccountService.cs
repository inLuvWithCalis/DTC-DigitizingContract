using ContractManagement.API.Domains.DTOs.Requests.Authentication;
using ContractManagement.API.Domains.DTOs.Responses.Authentication;

namespace ContractManagement.API.Domains.Interfaces.Authentication;

public interface IEmployeeAccountService
{
    Task<EmployeeProfileResponse> GetProfileAsync(
        int employeeId,
        CancellationToken cancellationToken = default);

    Task<EmployeeProfileResponse> UpdateProfileAsync(
        int employeeId,
        UpdateEmployeeSelfProfileRequest request,
        CancellationToken cancellationToken = default);

    Task<EmployeePreferencesResponse> GetPreferencesAsync(
        int employeeId,
        CancellationToken cancellationToken = default);

    Task<EmployeePreferencesResponse> UpdatePreferencesAsync(
        int employeeId,
        UpdateEmployeePreferencesRequest request,
        CancellationToken cancellationToken = default);

    Task ChangePasswordAsync(
        int employeeId,
        ChangeOwnPasswordRequest request,
        CancellationToken cancellationToken = default);

    Task<EmployeeProfileResponse> UploadProfileImageAsync(
        int employeeId,
        ProfileImageKind kind,
        ProfileImageUploadRequest request,
        CancellationToken cancellationToken = default);

    Task<EmployeeProfileResponse> DeleteProfileImageAsync(
        int employeeId,
        ProfileImageKind kind,
        string rowVersion,
        CancellationToken cancellationToken = default);

    Task<ProfileImageFile> OpenProfileImageAsync(
        int employeeId,
        ProfileImageKind kind,
        CancellationToken cancellationToken = default);
}
