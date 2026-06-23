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
export const quotationApi = {
  getAll: () => {
    return axiosClient.get<any, QuotationResponseDto[]>("/Quotation");
  },

  getById: (id: number) => {
    return axiosClient.get<any, QuotationResponseDto>(`/Quotation/${id}`);
  },

  create: (data: CreateQuotationRequestDto) => {
    return axiosClient.post<any, QuotationResponseDto>("/Quotation", data);
  },

  delete: (id: number) => {
    return axiosClient.delete<any, void>(`/Quotation/${id}`);
  },
};
