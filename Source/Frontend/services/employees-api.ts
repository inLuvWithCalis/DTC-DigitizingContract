import axiosClient from "@/lib/axios-interceptor";

export enum EmployeeType {
  Sale = 1,
  Marketing = 2,
  AdminOfficer = 3,
  Technical = 4,
  Accountant = 5,
  Manager = 6,
}

export enum EmployeeStatus {
  Active = 1,
  Inactive = 0,
}

export const getEmployeeTypeLabel = (type?: EmployeeType) => {
  switch (type) {
    case EmployeeType.Sale:
      return "Nhân viên Sale";
    case EmployeeType.Marketing:
      return "Marketing";
    case EmployeeType.AdminOfficer:
      return "Hành chính";
    case EmployeeType.Technical:
      return "Kỹ thuật";
    case EmployeeType.Accountant:
      return "Kế toán";
    case EmployeeType.Manager:
      return "Quản lý";
    default:
      return "Chưa cập nhật";
  }
};

export const getEmployeeStatusLabel = (status?: number) => {
  switch (status) {
    case EmployeeStatus.Active:
      return "Đang hoạt động";
    case EmployeeStatus.Inactive:
      return "Tạm khóa";
  }
};

export interface CreateEmployeeRequest {
  employeeCode?: string | null;
  employeeAccount: string;
  employeePassword: string;
  employeeFullName: string;
  employeeMobile?: string | null;
  employeeEmail?: string | null;
  departmentId?: number | null;
  employeeType?: EmployeeType | number | null;
}

export interface UpdateEmployeeRequest {
  employeeCode?: string | null;
  employeeFullName: string;
  employeeMobile?: string | null;
  employeeEmail?: string | null;
  departmentId?: number | null;
  employeeType?: EmployeeType | number | null;
}

export interface ChangePasswordRequest {
  newPassword: string;
}

export interface EmployeeResponse {
  employeeId: number;
  employeeCode?: string;
  employeeAccount: string;
  employeeFullName: string;
  employeeMobile?: string;
  employeeEmail?: string;
  departmentId?: number;
  employeeType?: EmployeeType;
  status?: number;
  createdDate?: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

const BASE_URL = "/admin/employees";

export const employeeApi = {
  getList: (params: {
    page?: number;
    pageSize?: number;
    keyword?: string;
    status?: number;
    dateCreated?: string;
  }) => {
    return axiosClient.get<any, PagedResult<EmployeeResponse>>(BASE_URL, {
      params,
    });
  },

  getById: (id: number) => {
    return axiosClient.get<any, EmployeeResponse>(`${BASE_URL}/${id}`);
  },

  create: (data: CreateEmployeeRequest) => {
    return axiosClient.post<any, EmployeeResponse>(BASE_URL, data);
  },

  update: (id: number, data: UpdateEmployeeRequest) => {
    return axiosClient.put<any, { employeeId: number }>(
      `${BASE_URL}/${id}`,
      data,
    );
  },

  changePassword: (id: number, data: ChangePasswordRequest) => {
    return axiosClient.put<any, { employeeId: number }>(
      `${BASE_URL}/${id}/password`,
      data,
    );
  },

  setStatus: (id: number, status: number) => {
    return axiosClient.patch<any, { employeeId: number; status: number }>(
      `${BASE_URL}/${id}/status`,
      null,
      {
        params: { status },
      },
    );
  },
};
