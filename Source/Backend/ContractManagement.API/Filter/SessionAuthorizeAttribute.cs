using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ContractManagement.Filter
{
    public class SessionAuthorizeAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // 1. Check if the session contains the "EmployeeId" and "TenantId" keys
            var session = context.HttpContext.Session;
            var employeeId = session.GetInt32("EmployeeId");
            var tenantId = session.GetInt32("TenantId");

            if (employeeId is null || tenantId is null)
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    message = "Unauthorized access. Please log in!"
                });
            }

            // 2. If the session contains the "EmployeeId" key, allow the action to execute
            base.OnActionExecuting(context);
        }
    }
}
