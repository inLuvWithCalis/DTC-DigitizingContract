import axios from "axios";

// Đọc base URL từ file .env.dev
const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL;

// Cấu hình một instance của axios
const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    "Content-Type": "application/json",
  },
});

// Thêm Interceptor nếu sau này bạn cần đính kèm Token (Authorization)
apiClient.interceptors.request.use((config) => {
  const token =
    typeof window !== "undefined" ? localStorage.getItem("auth_token") : null;
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// ============================================================================
// DTOs (Data Transfer Objects) mapping từ C# sang TypeScript
// ============================================================================

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
  quotationDate: string; // ISO 8601 Date String
  totalAmount: number;
  quatationStatus: string; // Lưu ý: Giữ nguyên typo "QuatationStatus" theo đúng backend C# của bạn
  items: QuotationItemResponse[];
}

// Request DTO cho việc POST tạo mới (Bỏ đi các trường tự sinh như Id, Amount...)
export interface CreateQuotationRequestDto {
  quotationNo: string;
  customerId: number;
  quotationDate: string;
  quatationStatus: string;
  items: Omit<QuotationItemResponse, "amount" | "productName">[];
}

// ============================================================================
// API Calls
// ============================================================================

export const quotationApi = {
  getAll: async (): Promise<QuotationResponseDto[]> => {
    const response = await apiClient.get<QuotationResponseDto[]>("/Quotation");
    return response.data;
  },

  getById: async (id: number): Promise<QuotationResponseDto> => {
    const response = await apiClient.get<QuotationResponseDto>(
      `/Quotation/${id}`,
    );
    return response.data;
  },

  create: async (
    data: CreateQuotationRequestDto,
  ): Promise<QuotationResponseDto> => {
    const response = await apiClient.post<QuotationResponseDto>(
      "/Quotation",
      data,
    );
    return response.data;
  },

  delete: async (id: number): Promise<void> => {
    await apiClient.delete(`/Quotation/${id}`);
  },
};
