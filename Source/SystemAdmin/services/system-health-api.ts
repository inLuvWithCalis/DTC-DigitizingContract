import axiosClient from "@/lib/axios-interceptor";

export interface DependencyHealth { status: string; code?: string | null }
export interface SystemHealthResponse {
  status: string;
  generatedAt: string;
  api: { status: string; version: string; startedAt: string; uptimeSeconds: number };
  centralDatabase: DependencyHealth;
  privateStorage: { status: string; writable: boolean; meetsCapacityThreshold: boolean; availableFreeSpaceBytes: number | null; minimumFreeSpaceBytes: number };
  pdfRenderer: { status: string; mode: string };
  otpDelivery: { status: string; providerMode: string; backlogCount: number | null; backlogCollection: string };
  sessionStore: { status: string; mode: string };
  failedTenantCount: number | null;
}

export const systemHealthApi = {
  get: () => axiosClient.get<unknown, SystemHealthResponse>("/api/admin/system-health"),
};
