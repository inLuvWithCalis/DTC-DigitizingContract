using ContractManagement.API.Common.Enums;

namespace ContractManagement.API.Common.Security;

/// <summary>
/// Public permission contract for the fixed Phase 7 RBAC model.
/// </summary>
public static class RbacPermissions
{
    public const string Version = "rbac-v1";

    public const string EmployeeDirectoryRead = "employee.directory.read";
    public const string EmployeeManage = "employee.manage";
    public const string DepartmentManage = "department.manage";
    public const string CatalogRead = "catalog.read";
    public const string CatalogManage = "catalog.manage";
    public const string CustomerLookup = "customer.lookup";
    public const string CustomerManage = "customer.manage";
    public const string QuotationManage = "quotation.manage";
    public const string ContractCreate = "contract.create";
    public const string ContractReadOwn = "contract.read.own";
    public const string ContractReadTenant = "contract.read.tenant";
    public const string ContractManageOwn = "contract.manage.own";
    public const string ContractApprovalDecide = "contract.approval.decide";
    public const string ContractComplete = "contract.complete";
    public const string ContractSupport = "contract.support";
    public const string TemplateAvailableRead = "template.available.read";
    public const string TemplateManage = "template.manage";
    public const string ContractAuditReadOwn = "contract-audit.read.own";
    public const string ContractAuditReadTenant = "contract-audit.read.tenant";
    public const string SecurityAuditReadTenant = "security-audit.read.tenant";
    public const string TenantLegalProfileManage = "tenant.legal-profile.manage";
    public const string FileAccessByResource = "file.access-by-resource";
}

/// <summary>
/// Fixed EmployeeType-to-permission mapping. UserRoles is intentionally not used.
/// </summary>
public static class EmployeePermissionCatalog
{
    private static readonly string[] BasePermissions =
    [
        RbacPermissions.EmployeeDirectoryRead,
        RbacPermissions.CatalogRead,
        RbacPermissions.CustomerLookup,
        RbacPermissions.QuotationManage,
        RbacPermissions.ContractCreate,
        RbacPermissions.ContractReadOwn,
        RbacPermissions.ContractManageOwn,
        RbacPermissions.TemplateAvailableRead,
        RbacPermissions.ContractAuditReadOwn,
        RbacPermissions.FileAccessByResource
    ];

    private static readonly IReadOnlyDictionary<EmployeeType, IReadOnlyList<string>> PermissionsByType =
        new Dictionary<EmployeeType, IReadOnlyList<string>>
        {
            [EmployeeType.Sale] = Merge(RbacPermissions.CustomerManage),
            [EmployeeType.Marketing] = Merge(RbacPermissions.CustomerManage),
            [EmployeeType.AdminOfficer] = Merge(
                RbacPermissions.CatalogManage,
                RbacPermissions.ContractSupport,
                RbacPermissions.TemplateManage,
                RbacPermissions.ContractAuditReadTenant),
            [EmployeeType.Technical] = Merge(),
            [EmployeeType.Accountant] = Merge(),
            [EmployeeType.Manager] = Merge(
                RbacPermissions.EmployeeManage,
                RbacPermissions.DepartmentManage,
                RbacPermissions.CatalogManage,
                RbacPermissions.CustomerManage,
                RbacPermissions.ContractReadTenant,
                RbacPermissions.ContractApprovalDecide,
                RbacPermissions.ContractComplete,
                RbacPermissions.ContractSupport,
                RbacPermissions.ContractAuditReadTenant,
                RbacPermissions.SecurityAuditReadTenant,
                RbacPermissions.TenantLegalProfileManage)
        };

    public static bool TryGetPermissions(
        byte? employeeTypeValue,
        out IReadOnlyList<string> permissions)
    {
        if (employeeTypeValue.HasValue
            && Enum.IsDefined(typeof(EmployeeType), employeeTypeValue.Value)
            && PermissionsByType.TryGetValue(
                (EmployeeType)employeeTypeValue.Value,
                out var mappedPermissions))
        {
            permissions = mappedPermissions;
            return true;
        }

        permissions = Array.Empty<string>();
        return false;
    }

    public static IReadOnlyList<string> GetPermissions(EmployeeType employeeType)
    {
        return PermissionsByType.TryGetValue(employeeType, out var permissions)
            ? permissions
            : Array.Empty<string>();
    }

    private static IReadOnlyList<string> Merge(params string[] additionalPermissions)
    {
        return Array.AsReadOnly(
            BasePermissions
                .Concat(additionalPermissions)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray());
    }
}
