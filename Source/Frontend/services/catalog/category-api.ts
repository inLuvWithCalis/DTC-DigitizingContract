import axiosClient from "@/lib/axios-interceptor";

export interface CategoryFilterParams {
  page?: number;
  pageSize?: number;
  keyword?: string | null;
}

export interface CreateCategoryRequest {
  categoryName: string;
  categoryShortDesc?: string | null;
  categoryOrder?: number | null;
  categoryParentId?: number | null;
  langId?: number | null;
  image?: string | null;
}

export interface UpdateCategoryRequest {
  categoryName: string;
  categoryShortDesc?: string | null;
  categoryOrder?: number | null;
  categoryParentId?: number | null;
  langId?: number | null;
  image?: string | null;
}

export interface CategoryResponse {
  categoryId: number;
  categoryName?: string | null;
  categoryShortDesc?: string | null;
  categoryOrder?: number | null;
  categoryParentId?: number | null;
  langId?: number | null;
  image?: string | null;
  productCount: number;
  items?: CategoryResponse[] | null;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

const BASE_URL = "/api/catalog/categories";

export const categoryApi = {
  getList: (params?: CategoryFilterParams) => {
    return axiosClient.get<any, PagedResult<CategoryResponse>>(BASE_URL, {
      params,
    });
  },
  getParents: (params?: CategoryFilterParams) => {
    return axiosClient.get<any, PagedResult<CategoryResponse>>(`${BASE_URL}/parents`, {
      params,
    });
  },
  getById: (id: number) => {
    return axiosClient.get<any, CategoryResponse>(`${BASE_URL}/${id}`);
  },
  create: (data: CreateCategoryRequest) => {
    return axiosClient.post<any, CategoryResponse>(BASE_URL, data);
  },
  update: (id: number, data: UpdateCategoryRequest) => {
    return axiosClient.put<any, { categoryId: number }>(`${BASE_URL}/${id}`, data);
  },
  delete: (id: number) => {
    return axiosClient.delete<any, { categoryId: number }>(`${BASE_URL}/${id}`);
  },
};
