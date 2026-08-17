using ContractManagement.API.Common.Enums;
using ContractManagement.API.Common.Security;

namespace ContractManagement.Tests.Domains.Security;

public sealed class EmployeePermissionCatalogTests
{
    public static IEnumerable<object[]> ExactPermissionMappings =>
    [
        [EmployeeType.Sale, Expected(RbacPermissions.CustomerManage)],
        [EmployeeType.Marketing, Expected(RbacPermissions.CustomerManage)],
        [EmployeeType.AdminOfficer, Expected(
            RbacPermissions.CatalogManage,
            RbacPermissions.ContractSupport,
            RbacPermissions.TemplateManage,
            RbacPermissions.ContractAuditReadTenant)],
        [EmployeeType.Technical, Expected()],
        [EmployeeType.Accountant, Expected()],
        [EmployeeType.Manager, Expected(
            RbacPermissions.EmployeeManage,
            RbacPermissions.DepartmentManage,
            RbacPermissions.CatalogManage,
            RbacPermissions.CustomerManage,
            RbacPermissions.ContractReadTenant,
            RbacPermissions.ContractSupport,
            RbacPermissions.ContractAuditReadTenant,
            RbacPermissions.SecurityAuditReadTenant)]
    ];

    [Theory]
    [MemberData(nameof(ExactPermissionMappings))]
    public void EveryFixedEmployeeType_HasOnlyItsApprovedPermissions(
        EmployeeType employeeType,
        IReadOnlyList<string> expectedPermissions)
    {
        var actualPermissions = EmployeePermissionCatalog.GetPermissions(employeeType);

        Assert.Equal(expectedPermissions, actualPermissions);
    }

    [Theory]
    [InlineData(EmployeeType.Sale)]
    [InlineData(EmployeeType.Marketing)]
    [InlineData(EmployeeType.AdminOfficer)]
    [InlineData(EmployeeType.Technical)]
    [InlineData(EmployeeType.Accountant)]
    [InlineData(EmployeeType.Manager)]
    public void EveryFixedEmployeeType_HasBasePermissions(EmployeeType employeeType)
    {
        var permissions = EmployeePermissionCatalog.GetPermissions(employeeType);

        Assert.Contains(RbacPermissions.EmployeeDirectoryRead, permissions);
        Assert.Contains(RbacPermissions.CatalogRead, permissions);
        Assert.Contains(RbacPermissions.CustomerLookup, permissions);
        Assert.Contains(RbacPermissions.QuotationManage, permissions);
        Assert.Contains(RbacPermissions.ContractCreate, permissions);
        Assert.Contains(RbacPermissions.ContractReadOwn, permissions);
        Assert.Contains(RbacPermissions.ContractManageOwn, permissions);
        Assert.Contains(RbacPermissions.TemplateAvailableRead, permissions);
        Assert.Contains(RbacPermissions.ContractAuditReadOwn, permissions);
        Assert.Contains(RbacPermissions.FileAccessByResource, permissions);
        Assert.Equal(permissions.Count, permissions.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void SaleAndMarketing_CanManageCustomers()
    {
        Assert.Contains(
            RbacPermissions.CustomerManage,
            EmployeePermissionCatalog.GetPermissions(EmployeeType.Sale));
        Assert.Contains(
            RbacPermissions.CustomerManage,
            EmployeePermissionCatalog.GetPermissions(EmployeeType.Marketing));
    }

    [Fact]
    public void AdminOfficer_HasCatalogTemplateAndContractSupportPermissions()
    {
        var permissions = EmployeePermissionCatalog.GetPermissions(
            EmployeeType.AdminOfficer);

        Assert.Contains(RbacPermissions.CatalogManage, permissions);
        Assert.Contains(RbacPermissions.TemplateManage, permissions);
        Assert.Contains(RbacPermissions.ContractSupport, permissions);
        Assert.Contains(RbacPermissions.ContractAuditReadTenant, permissions);
        Assert.DoesNotContain(RbacPermissions.EmployeeManage, permissions);
        Assert.DoesNotContain(RbacPermissions.ContractReadTenant, permissions);
    }

    [Fact]
    public void Manager_HasTenantGovernanceAndReadPermissions()
    {
        var permissions = EmployeePermissionCatalog.GetPermissions(EmployeeType.Manager);

        Assert.Contains(RbacPermissions.EmployeeManage, permissions);
        Assert.Contains(RbacPermissions.DepartmentManage, permissions);
        Assert.Contains(RbacPermissions.CatalogManage, permissions);
        Assert.Contains(RbacPermissions.CustomerManage, permissions);
        Assert.Contains(RbacPermissions.ContractReadTenant, permissions);
        Assert.Contains(RbacPermissions.ContractSupport, permissions);
        Assert.Contains(RbacPermissions.ContractAuditReadTenant, permissions);
        Assert.Contains(RbacPermissions.SecurityAuditReadTenant, permissions);
        Assert.DoesNotContain(RbacPermissions.TemplateManage, permissions);
    }

    [Theory]
    [InlineData(null)]
    [InlineData((byte)0)]
    [InlineData((byte)7)]
    [InlineData(byte.MaxValue)]
    public void NullOrInvalidEmployeeType_HasNoPermissions(byte? employeeType)
    {
        var found = EmployeePermissionCatalog.TryGetPermissions(
            employeeType,
            out var permissions);

        Assert.False(found);
        Assert.Empty(permissions);
    }

    private static IReadOnlyList<string> Expected(params string[] additionalPermissions)
    {
        string[] permissions =
        [
            RbacPermissions.CatalogRead,
            RbacPermissions.ContractAuditReadOwn,
            RbacPermissions.ContractCreate,
            RbacPermissions.ContractManageOwn,
            RbacPermissions.ContractReadOwn,
            RbacPermissions.CustomerLookup,
            RbacPermissions.EmployeeDirectoryRead,
            RbacPermissions.FileAccessByResource,
            RbacPermissions.QuotationManage,
            RbacPermissions.TemplateAvailableRead,
            .. additionalPermissions
        ];

        return permissions
            .Distinct(StringComparer.Ordinal)
            .OrderBy(permission => permission, StringComparer.Ordinal)
            .ToArray();
    }
}
