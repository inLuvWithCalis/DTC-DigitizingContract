import axiosClient from "@/lib/axios-interceptor";

const BASE_URL = "/admin/security-audits";

export const CENTRAL_SECURITY_AUDIT_ACTIONS = [
  "CentralApiAccessDenied",
  "SystemAdminLogin",
  "TenantProvisioned",
  "ManagerRoleChanged",
] as const;

export const CENTRAL_SECURITY_AUDIT_RESULTS = [
  "Success",
  "Denied",
  "Failed",
] as const;

export interface CentralSecurityAuditFilterRequest {
  page?: number;
  pageSize?: number;
  action?: string;
  result?: string;
  fromUtc?: string;
  toUtc?: string;
  tenantId?: number;
  tenantCode?: string;
  actorSystemAdminId?: number;
}

export interface CentralSecurityAuditResponse {
  centralSecurityAuditId: number;
  actorSystemAdminId?: number | null;
  tenantId?: number | null;
  tenantCode?: string | null;
  action: string;
  result: string;
  failureCode?: string | null;
  targetType?: string | null;
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

export interface CentralSecurityAuditPagedResult {
  items: CentralSecurityAuditResponse[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export const centralSecurityAuditApi = {
  getList: (params: CentralSecurityAuditFilterRequest) =>
    axiosClient.get<unknown, CentralSecurityAuditPagedResult>(BASE_URL, {
      params,
    }),
};
