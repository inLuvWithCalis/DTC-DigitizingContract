export const RBAC_PERMISSION_VERSION = "rbac-v1" as const;

export const RBAC_PERMISSIONS = {
  employeeDirectoryRead: "employee.directory.read",
  employeeManage: "employee.manage",
  departmentManage: "department.manage",
  catalogRead: "catalog.read",
  catalogManage: "catalog.manage",
  customerLookup: "customer.lookup",
  customerManage: "customer.manage",
  quotationManage: "quotation.manage",
  contractCreate: "contract.create",
  contractReadOwn: "contract.read.own",
  contractReadTenant: "contract.read.tenant",
  contractManageOwn: "contract.manage.own",
  contractApprovalDecide: "contract.approval.decide",
  contractComplete: "contract.complete",
  contractSupport: "contract.support",
  templateAvailableRead: "template.available.read",
  templateManage: "template.manage",
  contractAuditReadOwn: "contract-audit.read.own",
  contractAuditReadTenant: "contract-audit.read.tenant",
  securityAuditReadTenant: "security-audit.read.tenant",
  tenantLegalProfileManage: "tenant.legal-profile.manage",
  fileAccessByResource: "file.access-by-resource",
} as const;

export type RbacPermission =
  (typeof RBAC_PERMISSIONS)[keyof typeof RBAC_PERMISSIONS];

export const hasPermission = (
  permissions: readonly string[] | null | undefined,
  permission: RbacPermission,
) => permissions?.includes(permission) ?? false;

export const hasAnyPermission = (
  permissions: readonly string[] | null | undefined,
  requiredPermissions: readonly RbacPermission[],
) => requiredPermissions.some((permission) =>
  hasPermission(permissions, permission),
);
