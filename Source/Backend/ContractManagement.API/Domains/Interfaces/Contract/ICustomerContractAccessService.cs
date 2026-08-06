using ContractManagement.API.Domains.DTOs.Requests.Public;
using ContractManagement.API.Domains.DTOs.Responses.Public;

namespace ContractManagement.Domains.Interfaces.Contract;

/// <summary>
/// Narrow public access boundary for link, OTP and persisted customer sessions.
/// Contract comments themselves remain in IContractService's comment lifecycle.
/// </summary>
public interface ICustomerContractAccessService
{
    Task<CustomerOtpRequestAcceptedResponse> RequestOtpAsync(
        string linkToken,
        string suppliedPhoneNumber,
        CancellationToken cancellationToken = default);

    Task<CustomerAccessSessionIssue> VerifyOtpAsync(
        string linkToken,
        string publicChallengeId,
        string otp,
        CancellationToken cancellationToken = default);

    Task<CustomerSharedContractResponse> GetSharedAsync(
        string sessionSecret,
        CancellationToken cancellationToken = default);

    Task<CustomerPublicNegotiationCommentResponse> CreateCommentAsync(
        string sessionSecret,
        CreateCustomerNegotiationCommentRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record CustomerAccessSessionIssue(
    string SessionSecret,
    DateTime ExpiresAt);
