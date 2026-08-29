using ContractManagement.API.Common.Responses;
using ContractManagement.API.Common.Exceptions;
using ContractManagement.API.Common.Security;
using ContractManagement.API.Domains.Interfaces.Security;
using ContractManagement.Domains.Interfaces.ContractTemplate;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.Json;

namespace ContractManagement.Middleware
{
    /// <summary>
    /// Middleware bắt lỗi toàn cục.
    /// Mục đích:
    /// - Tránh try/catch lặp lại ở nhiều controller
    /// - Chuẩn hóa response lỗi trả về frontend
    /// - Log lỗi tập trung
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // Cho request đi tiếp qua các middleware/controller phía sau
                await _next(context);
            }
            catch (Exception ex)
            {
                // Nếu có exception chưa được xử lý, bắt tại đây
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(
            HttpContext context,
            Exception exception)
        {
            // Log lỗi để dev dễ debug
            _logger.LogError(exception, "Unhandled exception occurred.");

            // Mặc định lỗi chưa biết là 500
            var statusCode = HttpStatusCode.InternalServerError;
            var message = "Có lỗi xảy ra trong hệ thống.";

            if (exception is RbacOperationException rbacException)
            {
                await TryWriteTenantDeniedAuditAsync(
                    context,
                    rbacException.Code);
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = rbacException.StatusCode;
                await context.Response.WriteAsJsonAsync(
                    new AuthorizationErrorResponse(
                        rbacException.Code,
                        rbacException.Message));
                return;
            }

            if (exception is ContractTemplatePdfRenderingException pdfException)
            {
                context.Response.ContentType = "application/json";
                context.Response.StatusCode =
                    StatusCodes.Status503ServiceUnavailable;
                await context.Response.WriteAsJsonAsync(
                    new AuthorizationErrorResponse(
                        pdfException.FailureCode,
                        pdfException.Message));
                return;
            }

            if (exception is BusinessRuleException businessRuleException)
            {
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = businessRuleException.StatusCode;
                await context.Response.WriteAsJsonAsync(
                    new AuthorizationErrorResponse(
                        businessRuleException.Code,
                        businessRuleException.Message));
                return;
            }

            // Mapping một số exception phổ biến sang HTTP status phù hợp
            switch (exception)
            {
                case KeyNotFoundException:
                    context.Response.ContentType = "application/json";
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    await context.Response.WriteAsJsonAsync(
                        new AuthorizationErrorResponse(
                            AuthorizationErrorCodes.ResourceNotFound,
                            exception.Message));
                    return;

                case InvalidOperationException:
                    statusCode = HttpStatusCode.BadRequest;
                    message = exception.Message;
                    break;

                case UnauthorizedAccessException:
                    statusCode = HttpStatusCode.Unauthorized;
                    message = exception.Message;
                    break;

                case ArgumentException:
                    statusCode = HttpStatusCode.BadRequest;
                    message = exception.Message;
                    break;

                case DbUpdateConcurrencyException:
                    statusCode = HttpStatusCode.Conflict;
                    message = exception.Message;
                    context.Response.ContentType = "application/json";
                    context.Response.StatusCode = (int)statusCode;
                    await context.Response.WriteAsJsonAsync(
                        new AuthorizationErrorResponse(
                            AuthorizationErrorCodes.StaleRowVersion,
                            message));
                    return;
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            var response = ApiResponse<object>.Fail(message);

            var json = JsonSerializer.Serialize(
                response,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

            await context.Response.WriteAsync(json);
        }

        private static async Task TryWriteTenantDeniedAuditAsync(
            HttpContext context,
            string failureCode)
        {
            var target = SecurityAuditEndpointClassifier.GetTenantTarget(context);
            if (target is null)
            {
                return;
            }

            var writer = context.RequestServices
                .GetService<ITenantAuthorizationAuditWriter>();
            if (writer is null)
            {
                return;
            }

            await writer.TryWriteDeniedAsync(
                context,
                SecurityAuditHttpContextItems.GetDeniedActorEmployeeId(context),
                target.TargetType,
                target.TargetId,
                failureCode,
                context.RequestAborted);
        }
    }
}
