using ContractManagement.Attributes;
using ContractManagement.Infrastructure.MultiTenancy.Interfaces;
using ContractManagement.Infrastructure.MultiTenancy.Options;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;

namespace ContractManagement.Middleware.MultiTenancy;

/// <summary>
/// Xác định tenant cho mỗi HTTP request.
/// </summary>
public sealed class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly MultiTenancyOptions _options;

    public TenantResolutionMiddleware(
        RequestDelegate next,
        IOptions<MultiTenancyOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public async Task InvokeAsync(
      HttpContext context,
      ITenantResolver tenantResolver,
      ICurrentTenant currentTenant)
    {
        var isPublicCustomerAccess = context.GetEndpoint()?
            .Metadata
            .GetMetadata<PublicCustomerAccessAttribute>() is not null;

        // Public customer endpoints use tenantCode from the route, never employee session state.
        if (!context.Request.Path.StartsWithSegments("/api")
            && !isPublicCustomerAccess)
        {
            await _next(context);
            return;
        }

        bool allowWithoutTenant =
            context.GetEndpoint()?
                .Metadata
                .GetMetadata<AllowWithoutTenantAttribute>()
            is not null;

        if (allowWithoutTenant)
        {
            await _next(context);
            return;
        }

        /*
         * Lấy tenant đã lưu trong Session.
         *
         * Đây là nguồn ưu tiên đối với hệ thống
         * đang sử dụng Session Authentication.
         */
        var session = context.Features
            .Get<ISessionFeature>()?
            .Session;

        string? tenantFromSession =
            session?.GetString("TenantCode");

        /*
         * Nếu sau này dùng ASP.NET Authentication/JWT,
         * có thể lấy TenantCode từ claim đã xác thực.
         */
        string? tenantFromClaim =
            context.User
                .FindFirst(_options.TenantClaimType)?
                .Value;

        /*
         * Header chủ yếu dùng khi:
         *
         * - Đăng nhập lần đầu.
         * - Test Postman.
         * - Môi trường development.
         */
        string? tenantFromHeader = null;

        if (_options.AllowHeaderFallback)
        {
            tenantFromHeader =
                context.Request
                    .Headers[_options.HeaderName]
                    .FirstOrDefault();
        }

        /*
         * Khi Session đã có tenant nhưng header truyền tenant khác,
         * từ chối request thay vì cho người dùng đổi tenant.
         */
        if (!isPublicCustomerAccess
            && !string.IsNullOrWhiteSpace(tenantFromSession)
            && !string.IsNullOrWhiteSpace(tenantFromHeader)
            && !string.Equals(
                tenantFromSession,
                tenantFromHeader,
                StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode =
                StatusCodes.Status403Forbidden;

            await context.Response.WriteAsJsonAsync(
                new
                {
                    message =
                        "Tenant trong header không khớp "
                        + "với tenant của session."
                });

            return;
        }

        string? tenantCode = isPublicCustomerAccess
            ? context.Request.RouteValues["tenantCode"]?.ToString()
            : tenantFromSession
                ?? tenantFromClaim
                ?? tenantFromHeader;

        if (string.IsNullOrWhiteSpace(tenantCode))
        {
            context.Response.StatusCode =
                StatusCodes.Status400BadRequest;

            await context.Response.WriteAsJsonAsync(
                new
                {
                    message =
                        $"Thiếu tenant. Hãy truyền header "
                        + $"'{_options.HeaderName}'."
                });

            return;
        }

        var resolvedTenant =
            await tenantResolver.ResolveAsync(
                tenantCode,
                context.RequestAborted);

        if (resolvedTenant is null)
        {
            context.Response.StatusCode =
                StatusCodes.Status404NotFound;

            await context.Response.WriteAsJsonAsync(
                new
                {
                    message =
                        $"Tenant '{tenantCode}' không tồn tại "
                        + "hoặc chưa ở trạng thái Active."
                });

            return;
        }

        /*
         * Lưu tenant vào scoped CurrentTenant.
         *
         * DbDtctechContext được tạo sau đó sẽ lấy
         * connection string từ object này.
         */
        currentTenant.Set(resolvedTenant);

        await _next(context);
    }
}
