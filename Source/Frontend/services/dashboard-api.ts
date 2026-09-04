import axiosClient from "@/lib/axios-interceptor";

export interface DashboardFilter {
  from?: string;
  to?: string;
  expiryDays?: number;
}

export interface DashboardSummaryItem {
  key: string;
  count: number;
  previousCount: number | null;
}

export interface DashboardCurrencyAmount {
  currency: string;
  amount: number;
}

export interface DashboardVolumePoint {
  period: string;
  count: number;
}

export interface DashboardStatusPoint {
  status: string;
  count: number;
}

export interface ExpiringContract {
  contractId: number;
  contractCode: string;
  contractName: string;
  expiresAt: string;
  responsibleEmployeeName: string | null;
}

export interface RecentContractActivity {
  auditId: number;
  contractId: number;
  contractCode: string;
  action: string;
  actorDisplayName: string | null;
  occurredAt: string;
}

export interface DashboardResponse {
  scope: "Own" | "Tenant";
  generatedAt: string;
  fromUtc: string;
  toUtc: string;
  summary: DashboardSummaryItem[];
  amountByCurrency: DashboardCurrencyAmount[];
  volumeSeries: DashboardVolumePoint[];
  statusDistribution: DashboardStatusPoint[];
  expiringContracts: ExpiringContract[];
  recentActivities: RecentContractActivity[];
}

export const dashboardApi = {
  get: (filter: DashboardFilter = {}) =>
    axiosClient.get<unknown, DashboardResponse>("/api/dashboard", {
      params: filter,
    }),
};
