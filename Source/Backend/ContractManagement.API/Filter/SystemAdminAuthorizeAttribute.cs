using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ContractManagement.Filter;

public sealed class SystemAdminAuthorizeAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(
        ActionExecutingContext context)
    {
        int? systemAdminId =
            context.HttpContext.Session
                .GetInt32("SystemAdminId");

        if (systemAdminId is null)
        {
            context.Result = new UnauthorizedObjectResult(new
            {
                message = "System admin login required."
            });

            return;
        }

        base.OnActionExecuting(context);
    }
}