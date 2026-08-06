import axiosClient from "@/lib/axios-interceptor";

export enum CustomerInteractionType {
  Call = "Call",
  Email = "Email",
  Meeting = "Meeting",
  Zalo = "Zalo",
}

export const getCustomerInteractionTypeLabel = (
  type?: string | CustomerInteractionType | null,
) => {
  switch (type) {
    case CustomerInteractionType.Call:
    case "Call":
      return "Cuộc gọi";
    case CustomerInteractionType.Email:
    case "Email":
      return "Email";
    case CustomerInteractionType.Meeting:
    case "Meeting":
      return "Cuộc gặp / Meeting";
    case CustomerInteractionType.Zalo:
    case "Zalo":
      return "Zalo";
    default:
      return type || "Chưa cập nhật";
  }
};

export interface CreateCustomerInteractionRequest {
  interactionType: string;
  interactionSubject?: string | null;
  content?: string | null;
  nextFollowUpDate?: string | null;
}

export interface UpdateCustomerInteractionRequest {
  interactionType: string;
  interactionSubject?: string | null;
  content?: string | null;
  nextFollowUpDate?: string | null;
}

export interface CustomerInteractionResponse {
  interactionId: number;
  customerId: number;
  employeeId: number;
  employeeName?: string | null;
  interactionDate: string;
  interactionType: string;
  interactionSubject?: string | null;
  content?: string | null;
  nextFollowUpDate?: string | null;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

const getBaseUrl = (customerId: number) =>
  `/api/customers/${customerId}/interactions`;

export const customerInteractionApi = {
  getByCustomer: (customerId: number) => {
    return axiosClient.get<any, CustomerInteractionResponse[]>(
      getBaseUrl(customerId),
    );
  },
  create: (customerId: number, data: CreateCustomerInteractionRequest) => {
    return axiosClient.post<any, CustomerInteractionResponse>(
      getBaseUrl(customerId),
      data,
    );
  },
  update: (
    customerId: number,
    interactionId: number,
    data: UpdateCustomerInteractionRequest,
  ) => {
    return axiosClient.put<any, { customerId: number; interactionId: number }>(
      `${getBaseUrl(customerId)}/${interactionId}`,
      data,
    );
  },
};
