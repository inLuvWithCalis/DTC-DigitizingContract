using ContractManagement.API.Common.Security;
using ContractManagement.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace ContractManagement.Tests.Domains.Security;

public sealed class Slice06AuthorizationEvidenceTests
{
    [Fact]
    public async Task RbacStaleRowVersion_IsReturnedAsStable409Response()
    {
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new RbacOperationException(
                StatusCodes.Status409Conflict,
                AuthorizationErrorCodes.StaleRowVersion,
                "The employee was changed by another request."),
            NullLogger<ExceptionHandlingMiddleware>.Instance);
        var httpContext = new DefaultHttpContext
        {
            Response = { Body = new MemoryStream() }
        };

        await middleware.InvokeAsync(httpContext);

        Assert.Equal(StatusCodes.Status409Conflict, httpContext.Response.StatusCode);
        httpContext.Response.Body.Position = 0;
        var body = await new StreamReader(httpContext.Response.Body).ReadToEndAsync();
        Assert.Contains(AuthorizationErrorCodes.StaleRowVersion, body);
    }
}
