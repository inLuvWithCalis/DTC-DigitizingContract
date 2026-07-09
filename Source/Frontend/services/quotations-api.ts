import axiosClient from "@/lib/axios-interceptor";

export interface QuotationItemResponse {
  productId: number;
  productName: string;
  quantity: number;
  unitPrice: number;
  amount: number;
}

export interface QuotationResponseDto {
  quotationId: number;
  quotationNo: string;
  customerId: number;
  quotationDate: string;
  totalAmount: number;
  quatationStatus: string;
  items: QuotationItemResponse[];
}

export interface CreateQuotationRequestDto {
  quotationNo: string;
  customerId: number;
  quotationDate: string;
  quatationStatus: string;
  items: Omit<QuotationItemResponse, "amount" | "productName">[];
}

const BASE_URL = "/Quotation";

export const quotationApi = {
  getAll: () => {
    return axiosClient.get<any, QuotationResponseDto[]>(BASE_URL);
  },

  getById: (id: number) => {
    return axiosClient.get<any, QuotationResponseDto>(`${BASE_URL}/${id}`);
  },

  create: (data: CreateQuotationRequestDto) => {
    return axiosClient.post<any, QuotationResponseDto>(BASE_URL, data);
  },

  delete: (id: number) => {
    return axiosClient.delete<any, void>(`${BASE_URL}/${id}`);
  },
};
