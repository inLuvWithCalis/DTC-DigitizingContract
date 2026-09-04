import axiosClient from "@/lib/axios-interceptor";

export interface AdminDashboardSummary { key: string; count: number }
export interface CentralSecurityTrend { period: string; deniedCount: number; loginFailureCount: number }
export interface RecentTenant { tenantId: number; tenantCode: string; tenantName: string; status: string; createdAt: string }
export interface TenantProvisioningFailure { tenantId: number; tenantCode: string; tenantName: string; occurredAt: string; failureCode: string }
export interface RecentCentralAudit { auditId: number; action: string; result: string; actorDisplayName: string | null; tenantCode: string | null; occurredAt: string }

export interface AdminDashboardResponse {
  generatedAt: string;
  fromUtc: string;
  toUtc: string;
  summary: AdminDashboardSummary[];
  securitySeries: CentralSecurityTrend[];
  recentTenants: RecentTenant[];
  provisioningFailures: TenantProvisioningFailure[];
  recentAudits: RecentCentralAudit[];
}

export const adminDashboardApi = {
  get: (filter: { from?: string; to?: string } = {}) =>
    axiosClient.get<unknown, AdminDashboardResponse>("/api/admin/dashboard", { params: filter }),
};
