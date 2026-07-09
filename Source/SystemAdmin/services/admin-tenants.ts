import axiosClient from "@/lib/axios-interceptor";

export interface CreateTenantRequestDto {
  tenantCode: string;
  tenantName: string;
}

export interface TenantResponseDto {
  tenantId: string;
  tenantCode: string;
  tenantName: string;
  databaseName: string;
  databaseMode: string;
  status: string;
}

export const adminTenantsApi = {
  create: (payload: CreateTenantRequestDto) =>
    axiosClient.post<any, TenantResponseDto>("/admin/tenants", payload),
};
