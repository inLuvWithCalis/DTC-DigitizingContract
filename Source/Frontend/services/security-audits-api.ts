import axiosClient from "@/lib/axios-interceptor";

const BASE_URL = "/api/security-audits";

export const SECURITY_AUDIT_ACTIONS = [
  "AccessDenied",
  "EmployeeCreated",
  "EmployeeRoleChanged",
  "EmployeeStatusChanged",
  "EmployeePasswordReset",
  "ManagerRoleChanged",
] as const;

export const SECURITY_AUDIT_RESULTS = ["Success", "Denied", "Failed"] as const;

export interface TenantSecurityAuditFilterRequest {
  page?: number;
  pageSize?: number;
  action?: string;
  result?: string;
  fromUtc?: string;
  toUtc?: string;
  actorEmployeeId?: number;
}

export interface TenantSecurityAuditResponse {
  authorizationAuditId: number;
  tenantId: number;
  actorEmployeeId?: number | null;
  actorType: string;
  action: string;
  result: string;
  failureCode?: string | null;
  targetType: string;
  targetId?: string | null;
  previousEmployeeType?: number | null;
  newEmployeeType?: number | null;
  previousStatus?: number | null;
  newStatus?: number | null;
  occurredAt: string;
  ipAddress?: string | null;
  userAgent?: string | null;
  correlationId: string;
}

export interface SecurityAuditPagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export const securityAuditApi = {
  getList: (params: TenantSecurityAuditFilterRequest) =>
    axiosClient.get<unknown, SecurityAuditPagedResult<TenantSecurityAuditResponse>>(
      BASE_URL,
      { params },
    ),
};
