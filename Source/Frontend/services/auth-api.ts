import axiosClient from "@/lib/axios-interceptor";

const BASE_URL = "/api/Auth";

export interface LoginRequestDto {
  accountName: string;
  password?: string;
}

export interface LoginResponseDto {
  message: string;
  employeeId: number;
  employeeName: string | null;
}

export interface UserProfileDto {
  employeeId: number;
  employeeCode: string | null;
  employeeAccount: string;
  employeeFullName: string | null;
  titleId: number | null;
  employeeBirthDate: string | null;
  maritalStatus: number | null;
  gender: number | null;
  employeeMobile: string | null;
  employeePhone: string | null;
  employeeEmail: string | null;
  employeeAddress: string | null;
  userCreated: number | null;
  userModified: number | null;
  dateCreated: string | null;
  dateModified: string | null;
  hireDate: string | null;
  status: number | null;
  departmentId: number | null;
  others: string | null;
  defaultPage: string | null;
  employeeImageIcon: string | null;
  employeeType: number | null;
  userRoles: string | null;
  workTypeId: number | null;
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
