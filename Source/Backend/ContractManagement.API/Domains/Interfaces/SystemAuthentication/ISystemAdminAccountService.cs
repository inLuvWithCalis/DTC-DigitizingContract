using ContractManagement.API.Domains.DTOs.Requests.SystemAuthentication;
using ContractManagement.API.Domains.DTOs.Responses.SystemAuthentication;
using ContractManagement.API.Domains.DTOs.Requests.Authentication;
using ContractManagement.API.Domains.Interfaces.Authentication;

namespace ContractManagement.API.Domains.Interfaces.SystemAuthentication;

public interface ISystemAdminAccountService
{
    Task<SystemAdminProfileResponse> GetProfileAsync(
        int systemAdminId,
        CancellationToken cancellationToken = default);

    Task<SystemAdminProfileResponse> UpdateProfileAsync(
        int systemAdminId,
        UpdateSystemAdminProfileRequest request,
        CancellationToken cancellationToken = default);

    Task ChangePasswordAsync(
        int systemAdminId,
        ChangeSystemAdminPasswordRequest request,
        CancellationToken cancellationToken = default);

    Task<SystemAdminProfileResponse> UploadProfileImageAsync(
        int systemAdminId,
        ProfileImageKind kind,
        ProfileImageUploadRequest request,
        CancellationToken cancellationToken = default);

    Task<SystemAdminProfileResponse> DeleteProfileImageAsync(
        int systemAdminId,
        ProfileImageKind kind,
        string rowVersion,
        CancellationToken cancellationToken = default);

    Task<ProfileImageFile> OpenProfileImageAsync(
        int systemAdminId,
        ProfileImageKind kind,
        CancellationToken cancellationToken = default);
}
