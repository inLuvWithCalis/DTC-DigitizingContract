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

export const getEmployeeTypeLabel = (type?: EmployeeType | number | null) => {
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

export const getEmployeeStatusLabel = (status?: number | null) => {
  switch (status) {
    case EmployeeStatus.Active:
      return "Đang hoạt động";
    case EmployeeStatus.Inactive:
      return "Tạm khóa";
    default:
      return "Chưa xác định";
  }
};

export interface EmployeeFilterParams {
  page?: number;
  pageSize?: number;
  keyword?: string;
  categoryId?: number;
  status?: number;
  fromDate?: string;
  toDate?: string;
  dateCreated?: string;
}

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
  employeeCode?: string | null;
  employeeAccount?: string | null;
  employeeFullName?: string | null;
  employeeMobile?: string | null;
  employeeEmail?: string | null;
  departmentId?: number | null;
  departmentName?: string | null;
  employeeType?: EmployeeType | number | null;
  employeeTypeName?: string | null;
  status?: number | null;
  dateCreated?: string | null;
  createdDate?: string | null;
  dateModified?: string | null;
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
  getList: (params: EmployeeFilterParams) => {
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
