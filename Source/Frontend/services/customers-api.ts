import axiosClient from "@/lib/axios-interceptor";

export enum CustomerStatus {
  Active = 1,
  Inactive = 0,
}

export const getCustomerStatusLabel = (status?: number | null) => {
  switch (status) {
    case CustomerStatus.Active:
      return "Hoạt động";
    case CustomerStatus.Inactive:
      return "Ngừng hoạt động";
    default:
      return "Chưa xác định";
  }
};

export interface CustomerFilterParams {
  page?: number;
  pageSize?: number;
  keyword?: string;
  status?: number;
  fromDate?: string;
  toDate?: string;
}

export interface CreateCustomerRequest {
  customerCode?: string | null;
  customerFullName: string;
  customerCompany?: string | null;
  customerEmail?: string | null;
  customerMobile?: string | null;
  customerPhone?: string | null;
  customerFaxNumber?: string | null;
  customerTaxCode?: string | null;
  customerRepresentativeName?: string | null;
  customerRepresentativeTitle?: string | null;
  customerBankAccountNumber?: string | null;
  customerBankName?: string | null;
  customerAddress?: string | null;
  customerCity?: string | null;
  customerCountry?: string | null;
  customerWebsite?: string | null;
  customerNotes?: string | null;
}

export interface UpdateCustomerRequest {
  customerCode?: string | null;
  customerFullName: string;
  customerCompany?: string | null;
  customerEmail?: string | null;
  customerMobile?: string | null;
  customerPhone?: string | null;
  customerFaxNumber?: string | null;
  customerTaxCode?: string | null;
  customerRepresentativeName?: string | null;
  customerRepresentativeTitle?: string | null;
  customerBankAccountNumber?: string | null;
  customerBankName?: string | null;
  customerAddress?: string | null;
  customerCity?: string | null;
  customerCountry?: string | null;
  customerWebsite?: string | null;
  customerNotes?: string | null;
}

export interface CustomerResponse {
  customerId: number;
  customerCode?: string | null;
  customerFullName?: string | null;
  customerCompany?: string | null;
  customerEmail?: string | null;
  customerMobile?: string | null;
  customerPhone?: string | null;
  customerFaxNumber?: string | null;
  customerTaxCode?: string | null;
  customerRepresentativeName?: string | null;
  customerRepresentativeTitle?: string | null;
  customerBankAccountNumber?: string | null;
  customerBankName?: string | null;
  customerAddress?: string | null;
  customerCity?: string | null;
  customerCountry?: string | null;
  customerWebsite?: string | null;
  customerNotes?: string | null;
  status?: number | null;
  dateCreated?: string | null;
  dateModified?: string | null;
  totalContracts: number;
}

export interface CustomerLookupResponse {
  customerId: number;
  customerCode?: string | null;
  customerFullName?: string | null;
  customerCompany?: string | null;
  customerTaxCode?: string | null;
  customerMobile?: string | null;
  customerPhone?: string | null;
  status?: CustomerStatus | number | null;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

const BASE_URL = "/api/customers";

export const customerApi = {
  lookup: (keyword?: string) => {
    return axiosClient.get<any, CustomerLookupResponse[]>(
      `${BASE_URL}/lookup`,
      { params: { keyword: keyword || undefined } },
    );
  },
  getList: (params: CustomerFilterParams) => {
    return axiosClient.get<any, PagedResult<CustomerResponse>>(BASE_URL, {
      params,
    });
  },

  getById: (id: number) => {
    return axiosClient.get<any, CustomerResponse>(`${BASE_URL}/${id}`);
  },

  create: (data: CreateCustomerRequest) => {
    return axiosClient.post<any, CustomerResponse>(BASE_URL, data);
  },

  update: (id: number, data: UpdateCustomerRequest) => {
    return axiosClient.put<any, { customerId: number }>(
      `${BASE_URL}/${id}`,
      data,
    );
  },

  setStatus: (id: number, status: number) => {
    return axiosClient.patch<any, { customerId: number; status: number }>(
      `${BASE_URL}/${id}/status`,
      null,
      {
        params: { status },
      },
    );
  },
};
