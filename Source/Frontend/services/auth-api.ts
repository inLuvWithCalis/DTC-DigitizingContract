import axiosClient from "@/lib/axios-interceptor";
import type { RbacPermission } from "@/lib/rbac";
import type { EmployeeType } from "@/services/employees-api";

const BASE_URL = "/api/auth";

export interface LoginRequestDto {
  accountName: string;
  password?: string;
}

export interface LoginResponseDto {
  message: string;
  employeeId: number;
  employeeName: string | null;
  tenantId: number;
  tenantCode: string;
  tenantName: string;
}

export interface UserProfileDto {
  employeeId: number;
  account: string | null;
  fullName: string | null;
  employeeType: EmployeeType;
  roleName: string;
  tenantId: number;
  tenantCode: string;
  tenantName: string;
  permissionVersion: string;
  permissions: RbacPermission[];
}

export const authApi = {
  login: (payload: LoginRequestDto, tenantCode: string) => {
    return axiosClient.post<any, LoginResponseDto>(`${BASE_URL}/login`, payload, {
      withCredentials: true,
      headers: {
        "X-Tenant-Code": tenantCode,
      },
    });
  },

  getMe: () => {
    return axiosClient.get<any, UserProfileDto>(`${BASE_URL}/me`);
  },

  logout: async () => {
    await axiosClient.post<any, void>(
      `${BASE_URL}/logout`,
      {},
      {
        withCredentials: true,
      },
    );
  },
};
