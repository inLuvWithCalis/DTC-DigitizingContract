import axiosClient from "@/lib/axios-interceptor";
import {
  ApprovalRequestStatus,
  ContractStatus,
  type PagedResult,
} from "@/services/contract-api";

const BASE_URL = "/api/contract-approvals";

export interface ContractApprovalInboxFilterRequest {
  page?: number;
  pageSize?: number;
  keyword?: string;
}

export interface ContractApprovalArtifactResponse {
  fileId: number;
  fileName: string;
  fileType: string;
  contentType: string;
  fileSize: number;
  sha256: string;
}

export interface ContractApprovalRequestResponse {
  approvalRequestId: number;
  contractId: number;
  contractCode?: string | null;
  contractName: string;
  responsibleEmployeeId: number;
  responsibleEmployeeName?: string | null;
  versionId: number;
  versionNo: number;
  snapshotHash?: string | null;
  status: ApprovalRequestStatus;
  submittedByEmployeeId: number;
  submittedByEmployeeName?: string | null;
  submittedDate: string;
  resolvedByEmployeeId?: number | null;
  resolvedByEmployeeName?: string | null;
  resolvedDate?: string | null;
  decisionComment?: string | null;
  rowVersion: string;
}

export interface ContractApprovalDetailResponse
  extends ContractApprovalRequestResponse {
  artifacts: ContractApprovalArtifactResponse[];
}

export interface ContractApprovalActionResponse {
  approvalRequestId: number;
  contractId: number;
  versionId: number;
  approvalStatus: ApprovalRequestStatus;
  contractStatus: ContractStatus;
  resolvedByEmployeeId: number;
  resolvedDate: string;
  decisionComment?: string | null;
  approvalRequestRowVersion: string;
  contractRowVersion: string;
}

export interface ContractApprovalDecisionRequest {
  rowVersion: string;
  comment?: string | null;
}

export interface WithdrawContractApprovalRequest {
  rowVersion: string;
  reason: string;
}

export const contractApprovalApi = {
  getInbox: (params: ContractApprovalInboxFilterRequest) =>
    axiosClient.get<unknown, PagedResult<ContractApprovalRequestResponse>>(
      BASE_URL,
      { params },
    ),
  getDetail: (approvalRequestId: number) =>
    axiosClient.get<unknown, ContractApprovalDetailResponse>(
      `${BASE_URL}/${approvalRequestId}`,
    ),
  getContractHistory: (contractId: number) =>
    axiosClient.get<unknown, ContractApprovalRequestResponse[]>(
      `${BASE_URL}/contracts/${contractId}/history`,
    ),
  approve: (
    approvalRequestId: number,
    data: ContractApprovalDecisionRequest,
  ) =>
    axiosClient.post<unknown, ContractApprovalActionResponse>(
      `${BASE_URL}/${approvalRequestId}/approve`,
      data,
    ),
  returnForRevision: (
    approvalRequestId: number,
    data: ContractApprovalDecisionRequest,
  ) =>
    axiosClient.post<unknown, ContractApprovalActionResponse>(
      `${BASE_URL}/${approvalRequestId}/return`,
      data,
    ),
  reject: (
    approvalRequestId: number,
    data: ContractApprovalDecisionRequest,
  ) =>
    axiosClient.post<unknown, ContractApprovalActionResponse>(
      `${BASE_URL}/${approvalRequestId}/reject`,
      data,
    ),
  withdraw: (
    approvalRequestId: number,
    data: WithdrawContractApprovalRequest,
  ) =>
    axiosClient.post<unknown, ContractApprovalActionResponse>(
      `${BASE_URL}/${approvalRequestId}/withdraw`,
      data,
    ),
  downloadArtifact: (fileId: number) =>
    axiosClient.get<unknown, Blob>(`/api/files/${fileId}/download`, {
      responseType: "blob",
    }),
};
