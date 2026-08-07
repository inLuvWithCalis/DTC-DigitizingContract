import axiosClient from "@/lib/axios-interceptor";

const BASE_URL = "/api/contract-audits";

export const CONTRACT_AUDIT_ACTOR_TYPES = [
  "Employee",
  "Customer",
  "System",
] as const;

export const CONTRACT_AUDIT_RESULTS = [
  "Succeeded",
  "Failed",
  "Denied",
  "RateLimited",
  "ConcurrencyConflict",
] as const;

export const CONTRACT_AUDIT_ACTION_TYPES = [
  "ContractCreated",
  "ResponsibleAssigned",
  "ResponsibilityTransferred",
  "DraftUpdated",
  "NegotiationStarted",
  "NegotiationRoundCreated",
  "ExternalFeedbackCreated",
  "NegotiationReplyCreated",
  "NegotiationCommentResolved",
  "NegotiationCommentReopened",
  "VerificationPhoneSelected",
  "VerificationPhoneChanged",
  "CustomerAccessLinkCreated",
  "CustomerAccessLinkReplaced",
  "CustomerAccessLinkRevoked",
  "CustomerAccessLinkActivated",
  "CustomerAccessLinkInvalidated",
  "CustomerOtpRequested",
  "CustomerOtpSent",
  "CustomerOtpFailed",
  "CustomerOtpLocked",
  "CustomerOtpVerified",
  "CustomerSessionCreated",
  "CustomerSessionRevoked",
  "PublicVersionViewed",
  "CustomerCommentCreated",
  "CustomerCommentReplyCreated",
  "PublicAccessDenied",
  "ConcurrencyConflict",
] as const;

export type ContractAuditActorType =
  (typeof CONTRACT_AUDIT_ACTOR_TYPES)[number];
export type ContractAuditResult = (typeof CONTRACT_AUDIT_RESULTS)[number];
export type ContractAuditActionType =
  (typeof CONTRACT_AUDIT_ACTION_TYPES)[number];

export interface ContractAuditFilterRequest {
  contractId?: number;
  versionId?: number;
  actorType?: ContractAuditActorType;
  actionType?: ContractAuditActionType;
  result?: ContractAuditResult;
  fromUtc?: string;
  toUtc?: string;
  page?: number;
  pageSize?: number;
}

export interface ContractAuditResponse {
  contractAuditId: number;
  contractId: number;
  versionId: number | null;
  subjectType: string;
  subjectId: number;
  actorType: ContractAuditActorType;
  actorEmployeeId: number | null;
  actorCustomerAccessSessionId: number | null;
  actionType: ContractAuditActionType;
  result: ContractAuditResult;
  failureCode: string | null;
  previousValues: Record<string, unknown> | null;
  newValues: Record<string, unknown> | null;
  reason: string | null;
  occurredAt: string;
  ipAddress: string | null;
  userAgent: string | null;
  correlationId: string;
}

export interface ContractAuditPagedResult {
  items: ContractAuditResponse[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export const CONTRACT_AUDIT_ACTOR_LABELS: Record<
  ContractAuditActorType,
  string
> = {
  Employee: "Nhân viên",
  Customer: "Khách hàng",
  System: "Hệ thống",
};

export const CONTRACT_AUDIT_RESULT_LABELS: Record<ContractAuditResult, string> = {
  Succeeded: "Thành công",
  Failed: "Thất bại",
  Denied: "Bị từ chối",
  RateLimited: "Vượt giới hạn",
  ConcurrencyConflict: "Xung đột dữ liệu",
};

export const CONTRACT_AUDIT_ACTION_LABELS: Record<
  ContractAuditActionType,
  string
> = {
  ContractCreated: "Tạo hợp đồng",
  ResponsibleAssigned: "Gán nhân viên phụ trách",
  ResponsibilityTransferred: "Chuyển người phụ trách",
  DraftUpdated: "Cập nhật bản nháp",
  NegotiationStarted: "Bắt đầu đàm phán",
  NegotiationRoundCreated: "Tạo vòng đàm phán",
  ExternalFeedbackCreated: "Tạo phản hồi đàm phán",
  NegotiationReplyCreated: "Trả lời phản hồi đàm phán",
  NegotiationCommentResolved: "Đánh dấu đã xử lý phản hồi",
  NegotiationCommentReopened: "Mở lại phản hồi",
  VerificationPhoneSelected: "Chọn số điện thoại xác minh",
  VerificationPhoneChanged: "Đổi số điện thoại xác minh",
  CustomerAccessLinkCreated: "Tạo link truy cập khách hàng",
  CustomerAccessLinkReplaced: "Thay link truy cập khách hàng",
  CustomerAccessLinkRevoked: "Thu hồi link truy cập khách hàng",
  CustomerAccessLinkActivated: "Kích hoạt link truy cập",
  CustomerAccessLinkInvalidated: "Vô hiệu hóa link truy cập",
  CustomerOtpRequested: "Yêu cầu OTP khách hàng",
  CustomerOtpSent: "Gửi OTP khách hàng",
  CustomerOtpFailed: "Gửi OTP thất bại",
  CustomerOtpLocked: "Khóa OTP khách hàng",
  CustomerOtpVerified: "Xác minh OTP thành công",
  CustomerSessionCreated: "Tạo phiên khách hàng",
  CustomerSessionRevoked: "Thu hồi phiên khách hàng",
  PublicVersionViewed: "Khách hàng xem hợp đồng",
  CustomerCommentCreated: "Khách hàng gửi bình luận",
  CustomerCommentReplyCreated: "Khách hàng trả lời bình luận",
  PublicAccessDenied: "Từ chối truy cập công khai",
  ConcurrencyConflict: "Xung đột khi cập nhật",
};

export const contractAuditApi = {
  getList: (params: ContractAuditFilterRequest) =>
    axiosClient.get<unknown, ContractAuditPagedResult>(BASE_URL, { params }),
};
