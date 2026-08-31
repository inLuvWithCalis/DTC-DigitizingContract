import axiosClient from "@/lib/axios-interceptor";
import { ContractStatus } from "@/services/contract-api";

const BASE_URL = "/api/contracts";

export enum SignedEvidenceStatus {
  Active = 1,
  Superseded = 2,
}

export interface ContractSigningArtifactResponse {
  fileId: number;
  fileName: string;
  fileType: string;
  contentType: string;
  fileSize: number;
  sha256: string;
}

export interface ContractSignedEvidenceResponse {
  signedEvidenceId: number;
  contractId: number;
  versionId: number;
  versionNo: number;
  fileId: number;
  fileName: string;
  fileType: string;
  contentType: string;
  fileSize: number;
  sha256: string;
  status: SignedEvidenceStatus;
  providerSignerName: string;
  providerSignerTitle: string;
  providerSigningDate: string;
  customerSignerName: string;
  customerSignerTitle: string;
  customerSigningDate: string;
  supersedesEvidenceId?: number | null;
  supersedeReason?: string | null;
  uploadedByEmployeeId: number;
  uploadedByEmployeeName?: string | null;
  uploadedAt: string;
  supersededByEmployeeId?: number | null;
  supersededByEmployeeName?: string | null;
  supersededAt?: string | null;
  rowVersion: string;
}

export interface ContractSigningDetailResponse {
  contractId: number;
  contractStatus: ContractStatus;
  versionId: number;
  versionNo: number;
  versionLocked: boolean;
  contractRowVersion: string;
  versionRowVersion: string;
  approvedArtifacts: ContractSigningArtifactResponse[];
  activeEvidence?: ContractSignedEvidenceResponse | null;
  evidenceHistory: ContractSignedEvidenceResponse[];
}

export interface UploadContractSignedEvidenceRequest {
  file: File;
  currentVersionId: number;
  contractRowVersion: string;
  versionRowVersion: string;
  providerSignerName: string;
  providerSignerTitle: string;
  providerSigningDate: string;
  customerSignerName: string;
  customerSignerTitle: string;
  customerSigningDate: string;
}

export interface SupersedeContractSignedEvidenceRequest
  extends UploadContractSignedEvidenceRequest {
  evidenceRowVersion: string;
  reason: string;
}

const toFormData = (
  request:
    | UploadContractSignedEvidenceRequest
    | SupersedeContractSignedEvidenceRequest,
) => {
  const formData = new FormData();
  for (const [key, value] of Object.entries(request)) {
    formData.append(key, value instanceof File ? value : String(value));
  }
  return formData;
};

export const contractSigningApi = {
  get: (contractId: number) =>
    axiosClient.get<unknown, ContractSigningDetailResponse>(
      `${BASE_URL}/${contractId}/signing`,
    ),
  upload: (
    contractId: number,
    request: UploadContractSignedEvidenceRequest,
  ) =>
    axiosClient.post<unknown, ContractSignedEvidenceResponse>(
      `${BASE_URL}/${contractId}/signing/evidence`,
      toFormData(request),
      { headers: { "Content-Type": "multipart/form-data" } },
    ),
  supersede: (
    contractId: number,
    evidenceId: number,
    request: SupersedeContractSignedEvidenceRequest,
  ) =>
    axiosClient.post<unknown, ContractSignedEvidenceResponse>(
      `${BASE_URL}/${contractId}/signing/evidence/${evidenceId}/supersede`,
      toFormData(request),
      { headers: { "Content-Type": "multipart/form-data" } },
    ),
  downloadFile: (fileId: number) =>
    axiosClient.get<unknown, Blob>(`/api/files/${fileId}/download`, {
      responseType: "blob",
    }),
};
