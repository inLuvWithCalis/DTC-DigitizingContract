import axiosClient from "@/lib/axios-interceptor";

export interface CreateDepartmentRequest {
  departmentCode: string;
  departmentName: string;
  langId?: number | null;
}

export interface UpdateDepartmentRequest {
  departmentName: string;
  langId?: number | null;
}

export interface DepartmentResponse {
  departmentId: number;
  departmentCode: string;
  departmentName: string;
  modifiedDate?: string | null;
  status?: number | null;
  langId?: number | null;
}

const BASE_URL = "/admin/departments";

export const departmentApi = {
  getAll: () => {
    return axiosClient.get<any, DepartmentResponse[]>(BASE_URL);
  },

  getById: (id: number) => {
    return axiosClient.get<any, DepartmentResponse>(`${BASE_URL}/${id}`);
  },

  create: (data: CreateDepartmentRequest) => {
    return axiosClient.post<any, DepartmentResponse>(BASE_URL, data);
  },

  update: (id: number, data: UpdateDepartmentRequest) => {
    return axiosClient.put<any, { departmentId: number }>(
      `${BASE_URL}/${id}`,
      data,
    );
  },

  setStatus: (id: number, status: number) => {
    return axiosClient.patch<any, { departmentId: number; status: number }>(
      `${BASE_URL}/${id}/status`,
      null,
      {
        params: { status },
      },
    );
  },
};
