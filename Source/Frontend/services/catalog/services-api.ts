import axiosClient from "@/lib/axios-interceptor";

export enum ServiceStatus {
  Active = 1,
  Inactive = 0,
}

export const getServiceStatusLabel = (status?: number | null) => {
  switch (status) {
    case ServiceStatus.Active:
      return "Đang hoạt động";
    case ServiceStatus.Inactive:
      return "Ngừng hoạt động";
    default:
      return "Chưa cập nhật";
  }
};

export interface ServiceFilterParams {
  page?: number;
  pageSize?: number;
  keyword?: string | null;
  serviceTypeId?: number | null;
  status?: number | null;
  langId?: number | null;
  fromDate?: string | null;
  toDate?: string | null;
}

export interface CreateServiceRequest {
  serviceName: string;
  serviceTypeId?: number | null;
  serviceParentId?: number | null;
  servicePrice?: number | null;
  setupPrice?: number | null;
  maintainPrice?: number | null;
  langId?: number | null;
  serviceImageIcon?: string | null;
  serviceShortDesc?: string | null;
  serviceContent?: string | null;
  serviceOrder?: number | null;
  serviceRegion?: number | null;
  rewrite?: string | null;
  titleBrowser?: string | null;
  metaKeyword?: string | null;
  metaDescription?: string | null;
  others?: string | null;
}

export interface UpdateServiceRequest {
  serviceName: string;
  serviceTypeId?: number | null;
  serviceParentId?: number | null;
  servicePrice?: number | null;
  setupPrice?: number | null;
  maintainPrice?: number | null;
  langId?: number | null;
  serviceImageIcon?: string | null;
  serviceShortDesc?: string | null;
  serviceContent?: string | null;
  serviceOrder?: number | null;
  serviceRegion?: number | null;
  rewrite?: string | null;
  titleBrowser?: string | null;
  metaKeyword?: string | null;
  metaDescription?: string | null;
  others?: string | null;
}

export interface ServiceResponse {
  serviceId: number;
  serviceName?: string | null;
  serviceTypeId?: number | null;
  serviceTypeName?: string | null;
  serviceParentId?: number | null;
  servicePrice?: number | null;
  setupPrice?: number | null;
  maintainPrice?: number | null;
  status?: number | null;
  langId?: number | null;
  serviceImageIcon?: string | null;
  serviceShortDesc?: string | null;
  serviceContent?: string | null;
  serviceOrder?: number | null;
  serviceRegion?: number | null;
  rewrite?: string | null;
  titleBrowser?: string | null;
  metaKeyword?: string | null;
  metaDescription?: string | null;
  others?: string | null;
  userCreated?: number | null;
  userModified?: number | null;
  dateCreated?: string | null;
  dateModified?: string | null;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

const BASE_URL = "/catalog/services";

export const serviceApi = {
  getList: (params: ServiceFilterParams) => {
    return axiosClient.get<any, PagedResult<ServiceResponse>>(BASE_URL, {
      params,
    });
  },
  getById: (id: number) => {
    return axiosClient.get<any, ServiceResponse>(`${BASE_URL}/${id}`);
  },
  create: (data: CreateServiceRequest) => {
    return axiosClient.post<any, ServiceResponse>(BASE_URL, data);
  },
  update: (id: number, data: UpdateServiceRequest) => {
    return axiosClient.put<any, { serviceId: number }>(`${BASE_URL}/${id}`, data);
  },
  setStatus: (id: number, status: number) => {
    return axiosClient.patch<any, { serviceId: number; status: number }>(
      `${BASE_URL}/${id}/status`,
      null,
      { params: { status } },
    );
  },
  delete: (id: number) => {
    return axiosClient.delete<any, { serviceId: number }>(`${BASE_URL}/${id}`);
  },
};

// ==========================================
// ServiceType API & DTOs
// ==========================================
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

const SERVICE_TYPE_BASE_URL = "/catalog/service-types";

export const serviceTypeApi = {
  getList: (params: ServiceTypeFilterParams) => {
    return axiosClient.get<any, PagedResult<ServiceTypeResponse>>(
      SERVICE_TYPE_BASE_URL,
      { params },
    );
  },
  getById: (id: number) => {
    return axiosClient.get<any, ServiceTypeResponse>(
      `${SERVICE_TYPE_BASE_URL}/${id}`,
    );
  },
  create: (data: CreateServiceTypeRequest) => {
    return axiosClient.post<any, ServiceTypeResponse>(
      SERVICE_TYPE_BASE_URL,
      data,
    );
  },
  update: (id: number, data: UpdateServiceTypeRequest) => {
    return axiosClient.put<any, { serviceTypeId: number }>(
      `${SERVICE_TYPE_BASE_URL}/${id}`,
      data,
    );
  },
  delete: (id: number) => {
    return axiosClient.delete<any, { serviceTypeId: number }>(
      `${SERVICE_TYPE_BASE_URL}/${id}`,
    );
  },
};

