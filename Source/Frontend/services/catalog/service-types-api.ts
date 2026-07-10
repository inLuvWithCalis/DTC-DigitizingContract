import axiosClient from "@/lib/axios-interceptor";

export interface ServiceTypeFilterParams {
  page?: number;
  pageSize?: number;
  keyword?: string | null;
  langId?: number | null;
}

export interface CreateServiceTypeRequest {
  serviceTypeName: string;
  langId?: number | null;
}

export interface UpdateServiceTypeRequest {
  serviceTypeName: string;
  langId?: number | null;
}

export interface ServiceTypeResponse {
  serviceTypeId: number;
  serviceTypeName?: string | null;
  langId?: number | null;
  serviceCount: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

const BASE_URL = "/catalog/service-types";

export const serviceTypeApi = {
  getList: (params: ServiceTypeFilterParams) => {
    return axiosClient.get<any, PagedResult<ServiceTypeResponse>>(BASE_URL, {
      params,
    });
  },
  getById: (id: number) => {
    return axiosClient.get<any, ServiceTypeResponse>(`${BASE_URL}/${id}`);
  },
  create: (data: CreateServiceTypeRequest) => {
    return axiosClient.post<any, ServiceTypeResponse>(BASE_URL, data);
  },
  update: (id: number, data: UpdateServiceTypeRequest) => {
    return axiosClient.put<any, { serviceTypeId: number }>(`${BASE_URL}/${id}`, data);
  },
  delete: (id: number) => {
    return axiosClient.delete<any, { serviceTypeId: number }>(`${BASE_URL}/${id}`);
  },
};
