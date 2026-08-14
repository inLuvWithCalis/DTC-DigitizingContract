using System.Reflection;
using ContractManagement.API.Common.Security;
using ContractManagement.API.Domains.Controllers.CRM;
using ContractManagement.API.Domains.Controllers.Catalog;
using ContractManagement.Domains.Controllers.Catalog;
using ContractManagement.Domains.Controllers.CRM;
using ContractManagement.Domains.Controllers.ContractTemplate;
using ContractManagement.Domains.Controllers.Quotation;
using ContractManagement.Filter;

namespace ContractManagement.Tests.Domains.Security;

public sealed class Slice03PermissionEndpointTests
{
    [Theory]
    [InlineData(typeof(CategoryController), nameof(CategoryController.GetList))]
    [InlineData(typeof(CategoryController), nameof(CategoryController.GetParents))]
    [InlineData(typeof(CategoryController), nameof(CategoryController.GetById))]
    [InlineData(typeof(ProductController), nameof(ProductController.GetList))]
    [InlineData(typeof(ProductController), nameof(ProductController.GetById))]
    [InlineData(typeof(ServiceController), nameof(ServiceController.GetList))]
    [InlineData(typeof(ServiceController), nameof(ServiceController.GetById))]
    [InlineData(typeof(ServiceTypeController), nameof(ServiceTypeController.GetList))]
    [InlineData(typeof(ServiceTypeController), nameof(ServiceTypeController.GetById))]
    public void CatalogReadEndpoints_RequireCatalogRead(Type controllerType, string methodName)
    {
        Assert.Contains(RbacPermissions.CatalogRead,
            GetRequiredPermissions(controllerType.GetMethod(methodName)!));
    }

    [Theory]
    [InlineData(typeof(CategoryController), nameof(CategoryController.Create))]
    [InlineData(typeof(CategoryController), nameof(CategoryController.Update))]
    [InlineData(typeof(CategoryController), nameof(CategoryController.Delete))]
    [InlineData(typeof(ProductController), nameof(ProductController.Create))]
    [InlineData(typeof(ProductController), nameof(ProductController.Update))]
    [InlineData(typeof(ProductController), nameof(ProductController.SetStatus))]
    [InlineData(typeof(ProductController), nameof(ProductController.Delete))]
    [InlineData(typeof(ServiceController), nameof(ServiceController.Create))]
    [InlineData(typeof(ServiceController), nameof(ServiceController.Update))]
    [InlineData(typeof(ServiceController), nameof(ServiceController.SetStatus))]
    [InlineData(typeof(ServiceController), nameof(ServiceController.Delete))]
    [InlineData(typeof(ServiceTypeController), nameof(ServiceTypeController.Create))]
    [InlineData(typeof(ServiceTypeController), nameof(ServiceTypeController.Update))]
    [InlineData(typeof(ServiceTypeController), nameof(ServiceTypeController.Delete))]
    public void CatalogManageEndpoints_RequireCatalogManage(Type controllerType, string methodName)
    {
        Assert.Contains(RbacPermissions.CatalogManage,
            GetRequiredPermissions(controllerType.GetMethod(methodName)!));
    }

    [Theory]
    [InlineData(nameof(CustomerController.GetList))]
    [InlineData(nameof(CustomerController.GetById))]
    [InlineData(nameof(CustomerController.Create))]
    [InlineData(nameof(CustomerController.Update))]
    [InlineData(nameof(CustomerController.SetStatus))]
    public void FullCrmEndpoints_RequireCustomerManage(string methodName)
    {
        Assert.Contains(RbacPermissions.CustomerManage,
            GetRequiredPermissions(typeof(CustomerController).GetMethod(methodName)!));
    }

    [Fact]
    public void CustomerLookup_RequiresLookupRatherThanFullCrmPermission()
    {
        var permissions = GetRequiredPermissions(
            typeof(CustomerController).GetMethod(nameof(CustomerController.Lookup))!);

        Assert.Contains(RbacPermissions.CustomerLookup, permissions);
        Assert.DoesNotContain(RbacPermissions.CustomerManage, permissions);
    }

    [Theory]
    [InlineData(typeof(CustomerInteractionController), RbacPermissions.CustomerManage)]
    [InlineData(typeof(QuotationController), RbacPermissions.QuotationManage)]
    [InlineData(typeof(ContractTemplateController), RbacPermissions.TemplateManage)]
    [InlineData(typeof(ContractTemplateAvailableController), RbacPermissions.TemplateAvailableRead)]
    public void ControllerPermission_IsMappedToTheApprovedSlice03Permission(
        Type controllerType,
        string permission)
    {
        Assert.Contains(permission, GetRequiredPermissions(controllerType));
    }

    private static IReadOnlyList<string> GetRequiredPermissions(MemberInfo member)
    {
        var field = typeof(SessionAuthorizeAttribute).GetField(
            "_requiredPermissions",
            BindingFlags.Instance | BindingFlags.NonPublic)!;

        return member
            .GetCustomAttributes<SessionAuthorizeAttribute>()
            .SelectMany(attribute => (string[])field.GetValue(attribute)!)
            .ToArray();
    }
}
