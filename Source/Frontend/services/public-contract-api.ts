import { publicAxiosClient } from "@/lib/axios-interceptor";

export interface RequestCustomerAccessOtpRequest {
  phoneNumber: string;
}

export interface VerifyCustomerAccessOtpRequest {
  publicChallengeId: string;
  otp: string;
}

export interface CreateCustomerNegotiationCommentRequest {
  termId?: number | null;
  parentCommentId?: number | null;
  content: string;
}

export type CustomerNegotiationCommentSource =
  | "Customer"
  | "ExternalFeedback";

export type CustomerNegotiationCommentLifecycleState = "Open" | "Resolved";

export interface CustomerOtpRequestAcceptedResponse {
  publicChallengeId: string;
  deliveryChannel?: "Email" | "Sms";
}

export type CustomerAccessLinkAvailabilityState =
  | "Available"
  | "PendingActivation"
  | "Unavailable";

export interface CustomerAccessLinkAvailabilityResponse {
  isAvailable: boolean;
  state: CustomerAccessLinkAvailabilityState;
}

export interface CustomerPublicContractItemResponse {
  itemName: string;
  itemNameEn?: string | null;
  itemDescription?: string | null;
  quantity: number;
  unitName?: string | null;
  lineTotal: number;
  displayOrder: number;
}

export interface CustomerPublicContractTermResponse {
  termId: number;
  termCode: string;
  termTitle: string;
  termTitleEn?: string | null;
  termContent?: string | null;
  termContentEn?: string | null;
  isNegotiable: boolean;
  displayOrder: number;
}

export interface CustomerPublicNegotiationCommentResponse {
  commentId: number;
  termId?: number | null;
  parentCommentId?: number | null;
  content: string;
  source: CustomerNegotiationCommentSource;
  lifecycleState: CustomerNegotiationCommentLifecycleState;
  createdDate: string;
  updatedDate?: string | null;
}

export interface CustomerSharedContractResponse {
  contractCode?: string | null;
  contractName: string;
  contractNameEn?: string | null;
  effectiveDate?: string | null;
  expireDate?: string | null;
  currencyCode: string;
  totalAmount: number;
  items: CustomerPublicContractItemResponse[];
  terms: CustomerPublicContractTermResponse[];
  comments: CustomerPublicNegotiationCommentResponse[];
}

const BASE_URL = "/public/contracts";

const getTenantBaseUrl = (tenantCode: string) =>
  `${BASE_URL}/${encodeURIComponent(tenantCode)}`;

const getLinkBaseUrl = (tenantCode: string, linkToken: string) =>
  `${getTenantBaseUrl(tenantCode)}/${encodeURIComponent(linkToken)}`;

export const publicContractApi = {
  getLinkAvailability: (tenantCode: string, linkToken: string) => {
    return publicAxiosClient.get<any, CustomerAccessLinkAvailabilityResponse>(
      `${getLinkBaseUrl(tenantCode, linkToken)}/availability`,
    );
  },

  requestOtp: (
    tenantCode: string,
    linkToken: string,
    data: RequestCustomerAccessOtpRequest,
  ) => {
    return publicAxiosClient.post<any, CustomerOtpRequestAcceptedResponse>(
      `${getLinkBaseUrl(tenantCode, linkToken)}/otp/request`,
      data,
    );
  },

  verifyOtp: (
    tenantCode: string,
    linkToken: string,
    data: VerifyCustomerAccessOtpRequest,
  ) => {
    return publicAxiosClient.post<any, void>(
      `${getLinkBaseUrl(tenantCode, linkToken)}/otp/verify`,
      data,
    );
  },

  getShared: (tenantCode: string) => {
    return publicAxiosClient.get<any, CustomerSharedContractResponse>(
      `${getTenantBaseUrl(tenantCode)}/shared`,
    );
  },

  createComment: (
    tenantCode: string,
    data: CreateCustomerNegotiationCommentRequest,
  ) => {
    return publicAxiosClient.post<
      any,
      CustomerPublicNegotiationCommentResponse
    >(`${getTenantBaseUrl(tenantCode)}/comments`, data);
  },
};
