using ContractManagement.API.Common.Responses;
using ContractManagement.API.Domains.DTOs.Requests.Public;
using ContractManagement.API.Domains.DTOs.Responses.Public;
using ContractManagement.Attributes;
using ContractManagement.Domains.Interfaces.Contract;
using Microsoft.AspNetCore.Mvc;

namespace ContractManagement.Domains.Controllers.Contract;

/// <summary>
/// API công khai để khách hàng truy cập Current Version được nhân viên chia sẻ.
/// Luồng FE: xin OTP bằng link và số xác minh, xác thực OTP để nhận cookie HttpOnly, sau đó gọi
/// <c>shared</c> và <c>comments</c> với credentials. Cookie là customer session, không đọc, lưu,
/// hay tự tạo lại bằng JavaScript.
/// </summary>
[ApiController]
[Route("public/contracts/{tenantCode}")]
[PublicCustomerAccess]
public sealed class PublicCustomerContractAccessController : ControllerBase
{
    private const string CustomerAccessCookieName = "ContractManagement.CustomerAccess";

    private readonly ICustomerContractAccessService _customerAccessService;
    private readonly IWebHostEnvironment _environment;

    public PublicCustomerContractAccessController(
        ICustomerContractAccessService customerAccessService,
        IWebHostEnvironment environment)
    {
        _customerAccessService = customerAccessService;
        _environment = environment;
    }

    /// <summary>
    /// Xin OTP cho public link bằng số điện thoại đã được nhân viên chọn làm số xác minh.
    /// Link chờ kích hoạt vì hợp đồng còn Draft trả lỗi nghiệp vụ rõ ràng. Các trường hợp link,
    /// tenant hoặc phone không khớp vẫn trả accepted chung để không làm lộ thông tin. FE giữ public
    /// challenge ID trong response để gửi ở bước xác thực OTP.
    /// </summary>
    [HttpPost("{linkToken}/otp/request")]
    public async Task<IActionResult> RequestOtp(
        string tenantCode,
        string linkToken,
        [FromBody] RequestCustomerAccessOtpRequest request)
    {
        SetNoStore();
        var response = await _customerAccessService.RequestOtpAsync(
            linkToken,
            request.PhoneNumber,
            HttpContext.RequestAborted);

        return Accepted(ApiResponse<CustomerOtpRequestAcceptedResponse>.Ok(
            response,
            "Nếu thông tin hợp lệ, mã xác thực sẽ được gửi."));
    }

    /// <summary>
    /// Xác thực OTP theo public challenge và cấp customer-session cookie HttpOnly.
    /// Khi thành công, FE gọi <c>GET shared</c>; các request public tiếp theo phải gửi credentials.
    /// Không lưu OTP, challenge hoặc link trong browser storage.
    /// </summary>
    [HttpPost("{linkToken}/otp/verify")]
    public async Task<IActionResult> VerifyOtp(
        string tenantCode,
        string linkToken,
        [FromBody] VerifyCustomerAccessOtpRequest request)
    {
        SetNoStore();
        try
        {
            var issue = await _customerAccessService.VerifyOtpAsync(
                linkToken,
                request.PublicChallengeId,
                request.Otp,
                HttpContext.RequestAborted);
            WriteCustomerAccessCookie(issue.SessionSecret);
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return BadRequest(ApiResponse<object>.Fail(
                "Không thể xác thực mã. Hãy yêu cầu mã mới."));
        }
    }

    /// <summary>
    /// Trả về Contract, Current Version, nội dung có thể thương lượng và lịch sử trao đổi an toàn cho khách.
    /// API cần customer-session cookie được cấp sau OTP và làm mới thời gian không hoạt động của session.
    /// Response chủ ý không có danh tính nhân viên, dữ liệu phone, token, audit hoặc RowVersion.
    /// </summary>
    [HttpGet("shared")]
    public async Task<IActionResult> GetShared(string tenantCode)
    {
        SetNoStore();
        try
        {
            var shared = await _customerAccessService.GetSharedAsync(
                ReadCustomerAccessCookie(),
                HttpContext.RequestAborted);
            RefreshCustomerAccessCookie();
            return Ok(ApiResponse<CustomerSharedContractResponse>.Ok(shared));
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(ApiResponse<object>.Fail("Customer access is invalid or expired."));
        }
    }

    /// <summary>
    /// Tạo comment chung, comment Term có thể thương lượng hoặc reply trong Current Version được chia sẻ.
    /// FE không dùng API này để comment Item, sửa, xóa, resolve hoặc reopen comment. API cần
    /// customer-session cookie và làm mới thời gian không hoạt động của session khi thành công.
    /// </summary>
    [HttpPost("comments")]
    public async Task<IActionResult> CreateComment(
        string tenantCode,
        [FromBody] CreateCustomerNegotiationCommentRequest request)
    {
        SetNoStore();
        try
        {
            var comment = await _customerAccessService.CreateCommentAsync(
                ReadCustomerAccessCookie(),
                request,
                HttpContext.RequestAborted);
            RefreshCustomerAccessCookie();
            return Ok(ApiResponse<CustomerPublicNegotiationCommentResponse>.Ok(comment));
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(ApiResponse<object>.Fail("Customer access is invalid or expired."));
        }
    }

    private string ReadCustomerAccessCookie() =>
        Request.Cookies[CustomerAccessCookieName] ?? string.Empty;

    private void WriteCustomerAccessCookie(string value) =>
        Response.Cookies.Append(CustomerAccessCookieName, value, CreateCookieOptions());

    private void RefreshCustomerAccessCookie()
    {
        var value = ReadCustomerAccessCookie();
        if (!string.IsNullOrWhiteSpace(value))
        {
            WriteCustomerAccessCookie(value);
        }
    }

    private CookieOptions CreateCookieOptions() => new()
    {
        HttpOnly = true,
        Secure = _environment.IsProduction(),
        SameSite = SameSiteMode.Lax,
        IsEssential = true,
        Path = "/public/contracts"
    };

    private void SetNoStore()
    {
        Response.Headers.CacheControl = "no-store, no-cache, max-age=0";
        Response.Headers.Pragma = "no-cache";
    }
}
