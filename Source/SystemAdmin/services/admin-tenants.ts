import axiosClient from "@/lib/axios-interceptor";

export interface CreateTenantRequestDto {
  tenantCode: string;
  tenantName: string;
  initialManager: InitialManagerRequestDto;
}

export interface InitialManagerRequestDto {
  employeeCode?: string | null;
  employeeAccount: string;
  employeePassword: string;
  employeeFullName: string;
  employeeMobile?: string | null;
  employeeEmail?: string | null;
}

export enum TenantDatabaseMode {
  Dedicated = 1,
  Shared = 2,
}

export enum TenantStatus {
  Pending = 1,
  Provisioning = 2,
  Active = 3,
  Failed = 4,
  Suspended = 5,
}

export interface TenantResponseDto {
  tenantId: number;
  tenantCode: string;
  tenantName: string;
  databaseName: string;
  databaseMode: TenantDatabaseMode;
  status: TenantStatus;
}

export interface ChangeEmployeeRoleRequestDto {
  employeeType: number;
  rowVersion: string;
}

export interface ManagerGovernanceResponseDto {
  employeeId: number;
  employeeType: number;
  employeeTypeName: string;
  status: number;
  rowVersion: string;
}

export const adminTenantsApi = {
  getAll: () =>
    axiosClient.get<unknown, TenantResponseDto[]>("/api/admin/tenants"),
  create: (payload: CreateTenantRequestDto) =>
    axiosClient.post<unknown, TenantResponseDto>("/api/admin/tenants", payload),
  changeEmployeeRole: (
    tenantCode: string,
    employeeId: number,
    payload: ChangeEmployeeRoleRequestDto,
  ) =>
    axiosClient.put<unknown, ManagerGovernanceResponseDto>(
      `/api/admin/tenants/${encodeURIComponent(tenantCode)}/employees/${employeeId}/role`,
      payload,
    ),
};
