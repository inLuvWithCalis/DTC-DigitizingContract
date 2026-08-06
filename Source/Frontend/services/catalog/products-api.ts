import axiosClient from "@/lib/axios-interceptor";

export enum ProductStatus {
  Active = 1,
  Inactive = 0,
}

export const getProductStatusLabel = (status?: number) => {
  switch (status) {
    case ProductStatus.Active:
      return "Đang kinh doanh";
    case ProductStatus.Inactive:
      return "Ngừng kinh doanh";
    default:
      return "Chưa cập nhật";
  }
};

export interface ProductFilterParams {
  page?: number;
  pageSize?: number;
  keyword?: string;
  categoryId?: number;
  status?: number;
  fromDate?: string;
  toDate?: string;
}

export interface CreateProductRequest {
  productCode?: string | null;
  productName: string;
  categoryId?: number | null;
  productShortDesc?: string | null;
  productDetails?: string | null;
  productFeatures?: string | null;
  productBenefit?: string | null;
  productPrice?: number | null;
  productSmallImage?: string | null;
  productLargeImage?: string | null;
  langId?: number | null;
  productOrder?: number | null;
  productTags?: string | null;
  titleBrowser?: string | null;
  metaKeyword?: string | null;
  metaDescription?: string | null;
}

export interface UpdateProductRequest {
  productCode?: string | null;
  productName: string;
  categoryId?: number | null;
  productShortDesc?: string | null;
  productDetails?: string | null;
  productFeatures?: string | null;
  productBenefit?: string | null;
  productPrice?: number | null;
  productSmallImage?: string | null;
  productLargeImage?: string | null;
  langId?: number | null;
  productOrder?: number | null;
  productTags?: string | null;
  titleBrowser?: string | null;
  metaKeyword?: string | null;
  metaDescription?: string | null;
}

export interface ProductResponse {
  productId: number;
  productCode?: string;
  productName?: string;
  categoryId?: number;
  categoryName?: string;
  productShortDesc?: string;
  productDetails?: string;
  productFeatures?: string;
  productBenefit?: string;
  productPrice?: number;
  productSmallImage?: string;
  productLargeImage?: string;
  langId?: number;
  status?: number;
  productOrder?: number;
  productTags?: string;
  titleBrowser?: string;
  metaKeyword?: string;
  metaDescription?: string;
  productCreatedDate?: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

const BASE_URL = "/api/catalog/products";

export const productApi = {
  getList: (params: ProductFilterParams) => {
    return axiosClient.get<any, PagedResult<ProductResponse>>(BASE_URL, {
      params,
    });
  },

  getById: (id: number) => {
    return axiosClient.get<any, ProductResponse>(`${BASE_URL}/${id}`);
  },

  create: (data: CreateProductRequest) => {
    return axiosClient.post<any, ProductResponse>(BASE_URL, data);
  },

  update: (id: number, data: UpdateProductRequest) => {
    return axiosClient.put<any, { productId: number }>(
      `${BASE_URL}/${id}`,
      data,
    );
  },

  setStatus: (id: number, status: number) => {
    return axiosClient.patch<any, { productId: number; status: number }>(
      `${BASE_URL}/${id}/status`,
      null,
      {
        params: { status },
      },
    );
  },

  delete: (id: number) => {
    return axiosClient.delete<any, { productId: number }>(`${BASE_URL}/${id}`);
  },
};
